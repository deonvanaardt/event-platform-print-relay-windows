---
title: Windows Print Relay — Backlog
version: 1.0
date: 2026-07-01
status: active
owner: Founder
companion: docs/PRINT_RELAY_WINDOWS_PRD.md, INTEGRATION.md, Tech_Stack_Decision_Record.md
---

# Backlog — Windows Print Relay

Sprint-ready stories for the **event-platform-print-relay-windows** repository. Platform-side stories (E-05-S06–S09) live in the Event Platform monorepo.

**Definition of Done:** Acceptance criteria pass; unit tests green on macOS/Linux CI where applicable; Windows-only paths documented and manually verified on hardware before M4 closes.

---

## W-01 — Windows print relay MVP

**PRD:** `docs/PRINT_RELAY_WINDOWS_PRD.md` v3.0  
**Integration:** `INTEGRATION.md` · platform `printrelay/INTEGRATION.md`

| ID | Story | Acceptance (summary) |
|---|---|---|
| W-01-S01 | Repo scaffold + Core library | ✅ Done — API client, setup code codec, settings store, poll backoff; xUnit on macOS |
| W-01-S02 | WebView2 silent print spike (Gate 3) | ✅ Done — fixture HTML to named printer, no dialog; A5 physical sign-off |
| W-01-S03 | JSON Schema pinning + contract tests | ✅ Done — pinned `schemas/`; xUnit + CI |
| W-01-S04 | Poll loop + job lifecycle | ✅ Done — 1000 ms poll; complete/fail; backoff |
| W-01-S05 | Setup wizard (M1) | ✅ Done — `DESK-` code; printer dropdown; persist settings |
| W-01-S06 | Print `badge_html` from staging | ✅ Done — staging E2E smoke passed 2026-07-03 |
| W-01-S07 | System tray UI (M2) | ✅ Done — icon states; status panel; tray menu |
| W-01-S08 | Settings + diagnostics (M2) | ✅ Done — diagnostics export; JSON Lines log; log truncation |
| W-01-S09 | MSI + release CI (unsigned) (M3) | WiX `.msi`; HKCU auto-start; unsigned GitHub Release artifact |
| W-01-S11 | SignPath OSS signing CI (M3) | Signed `.msi` via SignPath; customer-ready GitHub Release |
| W-01-S10 | Physical sign-off (M4) | Win 10 + 11 with USB/network printer; version matrix in README |

### W-01-S03 — JSON Schema pinning + contract tests

- **Dependencies:** Platform E-05-S08 (shipped)
- **Acceptance:**
  - `schemas/` contains pinned copies of platform `schemas/print-relay/*.json` + fixtures.
  - `schemas/platform-pin.json` records platform `commit_sha`.
  - xUnit validates all fixtures against schemas; invalid samples rejected.
  - CI runs contract tests on every PR.

### W-01-S04 — Poll loop + job lifecycle

- **Dependencies:** W-01-S01
- **Acceptance:**
  - Poll interval 1000 ms (`RelayConstants.PollIntervalMs`).
  - Jobs processed in `created_at` order; one at a time.
  - `POST complete` / `POST failed` with `{ message }` max 500 chars.
  - Exponential backoff 2→60 s on connectivity failure; resume 1000 ms on success.
  - Never crash on single job failure.

### W-01-S05 — Setup wizard (M1)

- **Dependencies:** W-01-S03, platform staging with `badge_html`
- **Acceptance:**
  - Single paste field for `DESK-` setup code; decode `v: 1` payload.
  - Validate by calling `GET /api/print-queue/pending` with embedded secret.
  - Plain-English errors for network vs invalid code (PRD §5.2).
  - Printer dropdown from installed printers; persist to `%AppData%`.
  - Wizard skipped on subsequent launches when settings valid.

### W-01-S06 — Print `badge_html` from staging

- **Dependencies:** W-01-S02, W-01-S04, W-01-S05
- **Acceptance:**
  - Production path prints `badge_html` only via WebView2 (no `badge_document` layout).
  - Missing `badge_html` → `failed` with operator-safe message.
  - Page dimensions from HTML `@page` / CSS (CR80 default); `ShouldPrintBackgrounds = true`.
  - Integration test documented in `docs/STAGING_INTEGRATION.md`.

### W-01-S07 — System tray UI (M2)

- **PRD:** §7
- **Acceptance:**
  - Tray icon states and tooltips per PRD table.
  - Right-click menu: Status, Select printer, Print test badge, Copy diagnostics, Settings, Quit.
  - Starts minimised to tray after setup; no persistent main window.

### W-01-S08 — Settings + diagnostics (M2)

- **PRD:** §7.3, §9
- **Acceptance:**
  - Settings: desk name (read-only), printer dropdown, re-run setup, app version.
  - Never display secret, event ID, or full API URL.
  - Logs to `%AppData%\EventPlatform\PrintRelay\logs\relay.log` (JSON Lines); no secret leakage.
  - Log files truncate in place when over size cap (`relay.log` 5 MB, `startup.log` 256 KB).
  - Copy diagnostics JSON to clipboard per PRD §9.3.

### W-01-S09 — MSI + release CI (unsigned) (M3)

- **PRD:** §4.1, §6
- **Dependencies:** W-01-S06, W-01-S07
- **Acceptance:**
  - WiX produces `.msi` installing to `%ProgramFiles%\EventPlatform\PrintRelay\`.
  - Start Menu shortcut launches `EventPlatform.PrintRelay.exe`.
  - HKCU `Run` key for auto-start on login (installer-owned).
  - Finish screen confirms successful install; optional **Start Print Relay now** launches app on Finish (checked by default).
  - `release.yml` builds unsigned MSI on tag / workflow dispatch; artifact on GitHub Releases.
  - Manual install checklist in `docs/INSTALLER.md` passes on Windows hardware.
  - **No** `.pfx` / `signtool` secrets in CI.

### W-01-S11 — SignPath OSS signing CI (M3)

- **PRD:** §4.2
- **Dependencies:** W-01-S09; SignPath OSS approval
- **Acceptance:**
  - SignPath OSS project linked to this repo; signing policy for `.msi`.
  - `release.yml` submits unsigned MSI to `signpath/github-action-submit-signing-request`; publishes **signed** MSI to GitHub Releases.
  - Secrets: `SIGNPATH_API_TOKEN`, `SIGNPATH_ORG_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG` only.
  - Platform `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` points to signed release artifact (E-05-S09).

### W-01-S10 — Physical sign-off (M4)

- **PRD:** §11.3
- **Acceptance:**
  - Full flow on Windows 10 and 11 with physical printer (not PDF driver only).
  - README documents minimum platform version per Windows release.
  - Pilot checklist in `INTEGRATION.md` Phase D complete.

---

## Out of scope (this repo)

- Client-side badge rendering from `badge_document` JSON
- Offline / LAN print fallback
- Thermal / ZPL drivers
- Windows Service or machine-wide auto-start
- ARM64 Windows
- macOS/Linux builds of this .NET app
- Remote telemetry (post-MVP)

---

## Platform dependencies (not built here)

| Platform story | Required for |
|---|---|
| E-05-S06 `badge_html` on pending | W-01-S06 |
| E-05-S07 Copy setup code | W-01-S05 |
| E-05-S08 JSON Schema export | W-01-S03 |
| E-05-S09 Desk instructions + MSI URL | W-01-S11 go-live |
