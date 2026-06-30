---
title: Print Relay — Windows .NET Implementation PRD
version: 3.0
description: Product requirements for the Windows .NET print relay application. A non-technical user-facing desktop app for venue check-in desks.
date: 2026-06-30
status: draft
owner: Founder
parent-prd: Event Platform PRD v5.4
tags:
  - prd
  - print-relay
  - windows
  - dotnet
---

# Print Relay — Windows .NET Implementation

---

## 1. Purpose and scope

This document specifies requirements for a **Windows-native .NET desktop application** that acts as the silent print relay defined in Section 14.4 of the Event Platform PRD v5.4. It is a companion document to the parent PRD, not a replacement.

**Binding contracts (parent PRD):**

- Print relay HTTP API — Section 14.4 and Section 26 (four endpoints, desk scoping, idempotency, silent print semantics, additive `badge_html`, desk setup code `v: 1`).
- Desk setup code format — Section 5.2 of this document; registered in parent PRD Section 14.4 and Section 26.

**Rendering authority stays on the Event Platform.** Badge HTML is produced exclusively by `renderBadgeDocument` in `/lib/print` of the main repository. This Windows app does not implement badge layout logic, does not reimplement mm placement rules, and does not use a separate PDF layout library. It receives server-rendered HTML and prints it silently via WebView2.

The Windows relay performs the same runtime role as the Node.js relay in `/tools/print-relay` — poll the server-side print queue, print each badge silently to a pre-configured printer — but is designed for **non-technical venue staff**, not developers. The operator is assumed to have no terminal access, no awareness of UUIDs or secrets, and no ability to troubleshoot a command-line error. Setup must be achievable by anyone who can install a Windows app and paste a code.

**Platform split (to be reflected in parent PRD §14.4 amendment):**

| Platform | Delivery | Operator |
|---|---|---|
| macOS / Linux | Node.js relay in `/tools/print-relay` — one CLI command | Technical staff or integrators |
| Windows | Signed `.msi` desktop app (this document) | Non-technical venue staff |

The parent PRD currently describes only the Node.js relay. That text will be amended to list both delivery models. This document is authoritative for the Windows app; the parent PRD remains authoritative for API and queue semantics.

**Repository:** This application lives in a **separate repository**, not in the Event Platform monorepo. See Section 15.

---

## 2. Why a dedicated Windows app

The Node.js relay was designed for macOS/Linux with technically capable operators. Windows is a primary customer segment and faces the following blockers with the Node.js relay today:

- The Windows spooler path in `/tools/print-relay` uses `Start-Process -Verb Print`, which can invoke a system print dialog — a product defect at check-in.
- Node.js is not pre-installed on venue desk laptops.
- CLI-based setup (`--event-id`, `--secret`) is not viable for non-technical staff under event-day time pressure.
- IT departments at enterprise venues often restrict unsigned executables and non-standard runtimes.
- A terminal window left open on the desk is fragile and unprofessional.

A signed Windows installer with a tray-based UX addresses these and matches the standard of business software a venue operator would expect.

---

## 3. User profile

The primary operator is a **non-technical event staff member or venue technician**. They may:

- Be setting up the desk laptop on event morning with time pressure.
- Have received setup instructions from the event organiser, not from the platform vendor.
- Have no awareness of what an API, UUID, or relay secret is.
- Be using a laptop they do not own and cannot install arbitrary software on without a provided installer.

The application must not expose any of the following to the operator at any point: event IDs, relay secrets, API URLs, or any technical configuration string beyond the single setup code described in Section 5.2.

---

## 4. Installation

### 4.1 Installer format

The relay must ship as a standard Windows installer in `.msi` format. Double-clicking the installer must complete setup without requiring the operator to make technical decisions beyond accepting a UAC prompt. The installer must:

- Install the application to `%ProgramFiles%\EventPlatform\PrintRelay\` by default.
- Create a Start Menu entry.
- Register the application for auto-start on login for the installing user (see Section 6).
- Produce a single downloadable distribution artifact.

A `.exe` bootstrap wrapper (e.g. WiX Toolset or Advanced Installer) is acceptable if behaviour is equivalent.

**IT constraints:** Installing to Program Files requires administrator elevation once. If a venue laptop blocks per-machine installs, a **per-user install** under `%LocalAppData%\EventPlatform\PrintRelay\` may be offered as a documented alternative in a future release. MVP targets the standard per-machine MSI path; organiser setup docs must state that admin rights may be required once.

### 4.2 Code signing

The installer and application executable must be signed with a valid Authenticode certificate before any customer distribution. Unsigned builds trigger Windows SmartScreen and block installation for non-technical users.

Code signing is a hard gate before first customer deployment, not a post-MVP item. Requirements:

- Certificate ownership and renewal process documented before first release.
- CI pipeline signs release artifacts automatically — signing must not be a manual pre-event step.

### 4.3 Target platform

- Windows 10 (1803 or later) and Windows 11, x64.
- ARM64 support is post-MVP.
- No separate .NET runtime installation: the application is published self-contained.
- WebView2 Evergreen Runtime is required (see Section 8.2).

### 4.4 Distribution

MSI download URL and version pinning are owned by the Event Platform admin UI and event setup checklist (see Section 16). The Windows repository publishes signed MSI artifacts; the main platform links to them.

---

## 5. First-run setup

### 5.1 Setup wizard

On first launch after installation, the application presents a simple setup wizard with no more than two visible steps: paste setup code, then select printer.

### 5.2 Setup code

Each print desk has a cryptographically random relay secret shown once in the admin UI in the format `relay_<secret>`. The raw secret is never stored server-side after creation — only `relay_secret_hash` is persisted. The API resolves `event_id` and `desk_id` from the secret on every authenticated request, so the relay never needs the event ID in configuration.

The setup code is **not a new credential**. It packages the existing relay secret with the minimum context the Windows app needs, encoded as a single opaque string the operator pastes without understanding its contents.

**Setup code payload (version 1):**

```json
{
  "v": 1,
  "secret": "relay_k7mN2pQx9vR4wL8hJ3fT6yB1cD5",
  "api_url": "https://app.example.com",
  "desk_name": "Main entrance"
}
```

| Field | Required | Notes |
|---|---|---|
| `v` | yes | Format version. MVP uses `1` only. |
| `secret` | yes | Full `relay_…` desk secret. |
| `api_url` | yes | Platform origin — scheme + host, no trailing slash. Must match the admin session origin where the code was copied (`window.location.origin`), not a separate API subdomain unless the platform explicitly uses one. |
| `desk_name` | yes | Display only in tray and settings. Never used for auth. |

**Encoding:** Base64url of the JSON above, prefixed with `DESK-` (e.g. `DESK-eyJ2IjoxLCJzZWNyZXQ…`). Event ID is deliberately excluded.

**Invalidation:** Regenerating a desk secret via `POST /api/v1/events/{eventId}/print-desks/{deskId}/regenerate-secret` invalidates the old setup code immediately.

**Stability:** Once `v: 1` ships, the encoding format is a stable cross-repo contract. Any breaking change increments `v` and requires simultaneous updates to the admin UI encoder and this decoder. Registered in parent PRD Section 14.4 and Section 26.

**Admin UI (main repo):** Add a **Copy setup code** button wherever the raw relay secret is shown once (desk creation and secret regeneration). Encoding is client-side — no new API endpoint. Use the current browser origin as `api_url`.

**Wizard validation:** The operator sees one field: **Paste your desk setup code**. On Continue, the app decodes the code, extracts `secret` and `api_url`, and calls `GET /api/print-queue/pending` with `Authorization: Bearer <secret>`. Success proceeds to printer selection. Failure shows plain English:

- Network or 5xx: "Could not connect — check your internet connection and try again."
- 401 / 403: "Invalid setup code — contact your event organiser."

Warn organisers in admin copy: a code copied from staging will not work against production (wrong `api_url` or secret scope).

### 5.3 Printer selection

After successful setup code validation, the wizard shows a dropdown of printers installed on the machine (friendly names as in Devices and Printers). The operator selects the desk printer and clicks Finish.

The selected printer name is stored locally (`%AppData%\EventPlatform\PrintRelay\settings.json` or equivalent). It is not transmitted to the API.

### 5.4 Setup persistence

Once setup completes, configuration is stored locally. The wizard does not reappear on subsequent launches unless the operator chooses **Re-run setup** in settings. The relay starts polling automatically on launch.

---

## 6. Auto-start on login

The installer registers auto-start for the **user who ran the installer**, via registry `Run` key at `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. This is user-level, requires no elevation beyond install, and starts the app when that user logs in. A Windows Service is not required in MVP.

**Limitation:** If IT installs the MSI under a different Windows account than the desk operator, auto-start will not run for the operator. Setup documentation must instruct: install and log in as the desk user, or re-run setup after switching accounts. Machine-wide auto-start is post-MVP.

On startup, the application starts minimised to the system tray. No visible window opens unless setup is incomplete.

---

## 7. System tray

The relay runs as a system tray application. There is no persistent main window beyond the setup wizard and settings screen.

### 7.1 Tray icon states

| State | Icon indicator | Tooltip |
|---|---|---|
| Connected, polling | Green | "Print Relay — Connected" |
| Retrying after API error | Amber | "Print Relay — Reconnecting…" |
| Error (printer not found, invalid secret, repeated failure) | Red | "Print Relay — Error (click for details)" |
| Setup not complete | Grey | "Print Relay — Setup required" |

### 7.2 Tray right-click menu

| Item | Behaviour |
|---|---|
| Status | Opens a small status panel: desk name, printer name, jobs printed this session, last job time, connection state |
| Select printer | Printer dropdown; change without re-running full setup |
| Print test badge | Silent test print (see Section 8.5) |
| Copy diagnostics | Copies a support bundle to clipboard (see Section 9.3) — no secrets |
| Settings | Opens settings screen (Section 7.3) |
| Quit | Stops after completing any in-progress print attempt (Section 10) |

### 7.3 Settings screen

- Desk name (read-only, from setup code)
- Selected printer (editable dropdown)
- Re-run setup wizard (desk moved to a different event)
- Application version

The raw relay secret, event ID, and API URL must not be displayed.

---

## 8. Queue polling and print behaviour

### 8.1 Polling and job lifecycle

Align with the implemented Event Platform print queue API (`lib/print-queue` in the main repo).

- Poll `GET /api/print-queue/pending` every **1000 ms** (internal constant; not operator-configurable). Match the Node.js relay default.
- Authenticate with `Authorization: Bearer <relay_secret>` on every request.
- Process jobs sequentially in `created_at` order. No parallel printing in MVP.
- On successful print: `POST /api/print-queue/{id}/complete`.
- On print failure: `POST /api/print-queue/{id}/failed` with JSON body `{ "message": "<plain description>" }` only. Optional `message`; max 500 characters. No separate `error_type` field unless parent PRD Section 26 is amended.
- On API connectivity failure: exponential backoff (2 s, 4 s, 8 s, 16 s, cap 60 s). Tray icon amber. Resume 1000 ms polling on success; reset backoff.
- Never crash on a single job failure. Log, mark failed where appropriate, continue.

**Job states (platform today):** `queued` → `printed` | `failed`. There is no `processing` or claim step. A job remains `queued` until complete or failed is acknowledged.

**MVP operational rule:** Exactly **one relay instance per desk**. Running two instances with the same secret can cause duplicate prints. Crash mid-print leaves the job `queued`; restart may reprint. A future platform `printing` claim state is post-MVP.

**Pending response shape** (per job):

```json
{
  "id": "uuid",
  "status": "queued",
  "desk_id": "uuid",
  "event_id": "uuid",
  "registration_id": "uuid",
  "idempotency_key": "uuid",
  "is_reprint": false,
  "created_at": "ISO-8601",
  "badge_document": { },
  "badge_html": "<!DOCTYPE html>…"
}
```

`badge_html` is an **additive** field supplied by the platform (Section 16). The Windows app prints `badge_html` when present. If absent (older platform version), the app logs an error, marks the job failed with a clear message, and does not attempt client-side rendering from `badge_document`.

### 8.2 Badge rendering (WebView2 only)

**MVP uses one path only:** Microsoft Edge WebView2.

1. Load `badge_html` from the pending job response into a hidden WebView2 instance.
2. Print via `CoreWebView2.PrintAsync` with `CoreWebView2PrintSettings` targeting the configured printer by name.

**Not permitted in MVP:**

- Reimplementing layout in C# or a .NET PDF library (QuestPDF, PdfSharpCore, etc.).
- Client-side conversion from `badge_document` JSON to HTML.
- `ShellExecute` with the `print` verb (may invoke a dialog).

WebView2 Evergreen Runtime ships on Windows 10 1803+ and Windows 11 via Windows Update. If missing at startup, show a plain-English prompt with a link to install WebView2.

Output must match admin preview because HTML is produced by the platform's sole renderer (`renderBadgeDocument`). The Windows app honours:

- Physical dimensions from `@page` / CSS in the supplied HTML.
- `print-color-adjust: exact` semantics as already embedded in platform print CSS.
- No design-time guides in output (guaranteed server-side).

### 8.3 Silent printing

No system print dialog may appear during check-in or test print. A dialog is a product defect.

Silent printing is achieved via WebView2 `PrintAsync` with an explicit printer name. The app must not fall back to the system default printer silently.

### 8.4 Printer validation on startup

On every launch, after loading stored configuration:

1. Enumerate installed printers.
2. Verify the stored printer name exists.
3. If not found: red tray icon, notification ("Printer not found: [name]. Open Print Relay settings to select a printer."), suspend polling until a valid printer is selected.

### 8.5 Test print

**Print test badge** prints silently to the configured printer with no dialog. Purpose: verify paper, driver, and alignment before doors open.

Implementation: request a **test render from the platform** using the same path as production jobs — preferred approach is a dedicated relay-authenticated endpoint or a well-known test job pattern documented in the platform integration plan (Section 16). Until that exists, the app may load a **bundled fixture HTML** that uses the same `@page` dimensions as the default CR80 preset and displays "TEST PRINT", desk name, and a placeholder QR. Label the output clearly as a test, not a delegate badge.

Test print must not call `window.print()` in a way that surfaces a dialog.

---

## 9. Error handling, logging, and diagnostics

### 9.1 User-facing errors

Surface errors in plain English via Windows toast notifications or tray balloons. No stack traces, HTTP status codes, or UUIDs in operator-facing text.

| Error condition | User-facing message |
|---|---|
| API unreachable | "No connection — Print Relay is trying to reconnect" |
| Invalid or revoked secret | "Setup code is no longer valid — contact your event organiser" |
| Printer not found | "Printer not found — open Print Relay settings to select a printer" |
| Repeated print failure | "Badge could not print — check the printer is on and has paper" |
| Missing `badge_html` | "A badge could not be prepared for printing — contact support" |
| WebView2 missing | "Print Relay needs a Windows component — follow the prompt to install it" |

### 9.2 Local logs

Write JSON Lines to `%AppData%\EventPlatform\PrintRelay\logs\relay.log`:

- `timestamp`, `level`, `message`
- `job_id`, `registration_id` when applicable
- Never log the relay secret or full setup code

### 9.3 Diagnostics export

**Copy diagnostics** (tray menu) copies a JSON blob to the clipboard for organiser or support:

- App version
- Desk name (display name only)
- Printer name
- Connection state
- Last successful poll time
- Last job id and outcome
- Last error message (sanitised)
- WebView2 runtime version

Exclude: secret, setup code, full API URL (hostname only is acceptable).

Remote telemetry (e.g. Sentry) is post-MVP; local diagnostics satisfy MVP support needs and align with parent PRD observability goals for print-queue failures.

---

## 10. Graceful shutdown

On quit (tray menu or Windows session end):

- Finish the current print attempt if one is in progress, then call `complete` or `failed` as appropriate.
- Do not exit mid-spool without reporting job outcome where possible.
- Write a "Relay stopped" log entry.

Because the platform has no `processing` state, "in progress" is a client-side concept only. Prefer completing or failing the active job before exit.

---

## 11. Development and test environment

### 11.1 Cross-compilation from macOS

Develop on macOS using the .NET SDK. Cross-compile to Windows:

```
dotnet publish -r win-x64 --self-contained true -p:PublishSingleFile=true -c Release
```

CI must not assume a Windows build host for compile and unit tests. WiX v4 (or equivalent) for `.msi` packaging must run in CI or a reproducible build environment.

### 11.2 Testable on macOS (xUnit)

- Setup code encode/decode (`v: 1`)
- Queue polling logic, backoff, retry
- Pending response parsing (including `badge_html` presence check)
- API auth header construction
- Fail payload construction (`{ message }` only)
- Settings persistence
- Log and diagnostics format (no secret leakage)

### 11.3 Requires Windows hardware or VM

- System tray UI
- WebView2 silent print to named printer
- Printer enumeration
- MSI install and HKCU auto-start
- SmartScreen with signed vs unsigned builds
- End-to-end flow with a real printer

**Gate before first customer deployment:** full end-to-end on a physical Windows machine with a real printer. VM with "Microsoft Print to PDF" is acceptable for layout iteration; physical printer required for go-live sign-off.

### 11.4 Recommended local test setup

- Windows 11 VM (Parallels or UTM) for print path and tray UI iteration
- Microsoft Print to PDF for layout-only checks
- Physical USB or network printer for pre-deployment sign-off
- Event Platform staging environment for live `badge_html` integration tests

---

## 12. Pre-coding gates

Resolve before significant implementation:

1. **Renderer coupling approved** — Parent PRD and platform team agree to additive `badge_html` on `GET /api/print-queue/pending` (Section 16). Alternative: published `@emp/print-renderer` package — only if server-side HTML is rejected.
2. **Setup code `v: 1` frozen** — JSON schema documented; registered in parent PRD Section 26.
3. **WebView2 silent print spike** — Physical Windows + named printer, no dialog, using fixture HTML matching CR80 dimensions.
4. **Code-signing certificate** — Available or on order; CI signing path defined.
5. **Contract verification** — Windows app types and tests aligned with `lib/print-queue/serialize.ts` response shapes in the main repo (not a greenfield API design — endpoints already ship).
6. **JSON Schema export** — `BadgeRenderInput` / pending job response schema published from main repo for deserialisation validation in the Windows repo.

Do not build MSI packaging or polish tray UX until gate 3 passes.

---

## 13. Out of scope (this PRD)

- Offline / LAN-fallback printing (post-MVP — parent PRD Section 14.15)
- Thermal / ZPL printer support (post-MVP)
- Windows Service or machine-wide auto-start (post-MVP)
- ARM64 Windows (post-MVP)
- macOS or Linux builds of this .NET app (Node.js relay covers those platforms)
- Multi-desk management from one relay instance (one relay per desk)
- Client-side badge rendering from `badge_document` JSON
- Platform `printing` / claim job state (post-MVP platform improvement)
- Remote error telemetry (post-MVP)

---

## 14. Stability and compatibility

**Print relay HTTP API** — Subject to parent PRD Section 26. The four endpoints, desk scoping, idempotency behaviour, and silent print semantics must not diverge between the Node.js relay and this Windows app without a parent PRD update first.

**Additive `badge_html` field** — Treated as a backward-compatible extension. Node.js relay may ignore it and continue using `renderBadgeDocument` locally until updated.

**Desk setup code `v: 1`** — Stable cross-repo contract between admin UI and this app. Breaking changes increment `v`. Registered in parent PRD Section 14.4 and Section 26.

**App ↔ platform version compatibility** — Document minimum platform version per Windows app release (e.g. "Print Relay 1.2 requires platform ≥ 0.x.y for `badge_html`"). Enforced in release notes, not runtime blocking in MVP unless `badge_html` is absent.

---

## 15. Repository boundaries

| Concern | Owner |
|---|---|
| Windows tray app, MSI, code signing, WebView2 print | **Separate repo** (e.g. `event-platform-print-relay-windows`) |
| Print queue API, `badge_html` rendering, desk setup code UI | **Event Platform monorepo** |
| `renderBadgeDocument`, badge types, queue serialize types | **Event Platform monorepo** (`/lib/print`, `/lib/print-queue`) |
| Node.js relay (macOS/Linux) | **Event Platform monorepo** (`/tools/print-relay`) |

**Type sync:** The main repo publishes versioned JSON Schema (or OpenAPI components) for pending job responses and setup code `v: 1`. The Windows repo validates against published schema in CI — no hand-copied TypeScript structs.

**Release cadence:** Windows app releases are independent of platform deploys but must declare compatible platform versions. Platform changes to `badge_html` or setup code format may require a coordinated release.

---

## 16. Platform changes required (main repo)

Track in Event Platform backlog / parent PRD. See `printrelay/INTEGRATION.md` for the two-project checklist.

| Change | Purpose |
|---|---|
| Add `badge_html` to `GET /api/print-queue/pending` responses | Server-side `renderBadgeDocument(job.badge_document, { mode: 'print' })` — single renderer authority |
| **Copy setup code** in print desk UI (create + regenerate flows) | Encode `v: 1` payload with `window.location.origin` |
| Update print desk setup instructions | Windows MSI path + optional CLI path for macOS/Linux |
| Parent PRD §14.4 amendment | Windows MSI + Node CLI as dual delivery; optional `--event-id` deprecation for Node relay |
| Parent PRD §26 amendment | Desk setup code `v: 1` stability entry |
| Export JSON Schema for pending job + `BadgeRenderInput` | Windows repo CI validation |
| Event setup checklist link | Download signed MSI; paste setup code per desk |
| Test print for relay (optional MVP) | Relay-authenticated test HTML endpoint or documented fixture contract |

**Preferred `badge_html` implementation sketch** (for platform planning only):

```typescript
// In listPendingPrintJobs / serialize — illustrative
badge_html: renderBadgeDocument(row.badge_document, { mode: "print" }),
```

Node relay can migrate to `badge_html` when convenient; Windows app depends on it from first release.

---

## 17. Integration checklist (two-project plan)

Use this when both PRDs are approved and implementation starts.

1. Freeze setup code `v: 1` and merge parent PRD §26 entry.
2. Ship `badge_html` on pending endpoint in staging; verify with integration test.
3. Publish JSON Schema artifact from platform CI.
4. Windows repo: decode setup code, poll staging, print `badge_html` via WebView2 spike.
5. Admin UI: Copy setup code + updated desk instructions.
6. Windows repo: tray app, settings, diagnostics, MSI, signed CI release.
7. Event setup checklist: MSI download + per-desk setup code steps.
8. Physical printer sign-off on Windows 10 and 11.
9. Document compatible version matrix in both repos' README / release notes.

---

## 18. Document history

| Version | Date | Notes |
|---|---|---|
| 2.0 | 2026-06-29 | Initial Windows .NET PRD draft |
| 3.0 | 2026-06-30 | Single renderer via server `badge_html`; setup code `v: 1`; API alignment; repo boundaries; platform change list; removed client-side PDF option |
