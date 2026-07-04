---
title: Tech Stack Decision Record — Windows Print Relay
version: 1.0
date: 2026-07-01
status: active
owner: Founder
companion: docs/PRINT_RELAY_WINDOWS_PRD.md, INTEGRATION.md
---

# Tech Stack Decision Record — Windows Print Relay

Tools chosen for the signed Windows tray app. The parent Event Platform [Tech Stack Decision Record](https://github.com/deonvanaardt/event-management-platform/blob/main/Tech_Stack_Decision_Record.md) governs the monorepo; **this document governs the Windows repo only**.

Do not substitute, add, or remove tools without updating this record.

---

## 1. Core stack

| Layer | Tool | Version constraint | Notes |
|---|---|---|---|
| Runtime | .NET | 8.0 LTS (`global.json`) | Self-contained publish; no separate runtime install |
| Language | C# | `LangVersion: latest` | Nullable enabled; `TreatWarningsAsErrors` on all projects |
| Test framework | xUnit | Latest stable | Runs on macOS/Linux CI for Core |
| Windows UI (MVP) | WinForms + WebView2 | `Microsoft.Web.WebView2` pinned in csproj | Hidden host form for print; tray icon via `NotifyIcon`. WPF allowed for wizard if needed — pick one surface, document here |
| HTTP client | `HttpClient` | BCL | `PrintRelayApiClient` in Core; no RestSharp |
| JSON | `System.Text.Json` | BCL | Property names case-insensitive for API responses |
| Schema validation | JsonSchema.Net | Latest stable | Contract tests against pinned `schemas/*.json` |
| CI | GitHub Actions | — | Core tests ubuntu; Windows build job for Spike/App |
| Installer | WiX Toolset v5 (`WixToolset.Sdk`) | 5.0.x | Produces `.msi`; folder-publish harvest; `WixToolset.UI.wixext` (`WixUI_Minimal`) + `WixToolset.Util.wixext` (launch-on-finish); see `installer/` |
| Code signing | SignPath OSS | — | Authenticode output via SignPath API (W-01-S11); **no** self-managed `.pfx`. Secrets: `SIGNPATH_API_TOKEN`, `SIGNPATH_ORG_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG` |

---

## 2. Print path

| Concern | Approach | Notes |
|---|---|---|
| Badge layout authority | **Platform server only** | Print `badge_html` from `GET /api/print-queue/pending`. Never render from `badge_document` JSON |
| HTML engine | WebView2 Evergreen | Load `badge_html` in hidden WebView2 |
| Silent print | Named printer, no dialog | PRD §8.3 — dialog is a product defect |
| Primary API (target) | `CoreWebView2.PrintAsync` | Per Windows PRD §8.2 when driver supports it |
| Spike / driver fallback | `PrintToPdfAsync` + Pdfium spooler | Spike-proven for some GDI drivers; validate CR80 on production path before M4 (see `DECISIONS.md`) |
| Page size | From server HTML `@page` / CSS | `ShouldPrintBackgrounds = true`; zero margins |
| Test print | Bundled fixture HTML | Until platform relay test endpoint exists (PRD §8.5) |

### Do NOT introduce

- QuestPDF, PdfSharpCore, iText, or any client-side badge layout library
- `ShellExecute` print verb (may show dialog)
- Client-side conversion from `badge_document` to HTML
- Thermal/ZPL SDKs (post-MVP)
- Second copy of platform badge renderer logic

**PdfiumPrinter** is permitted only as a **spooler** for PDF bytes already rendered by WebView2 — not for layout.

---

## 3. Project structure

```
src/
  EventPlatform.PrintRelay.Core/       API client, setup code, poll loop, settings, logging interfaces
  EventPlatform.PrintRelay.App/        Tray app + wizard (Phase 1–2) — Windows only
  EventPlatform.PrintRelay.Spike/      Gate 3 CLI — keep for print regression until App ships
tests/
  EventPlatform.PrintRelay.Core.Tests/ xUnit + schema contract tests
schemas/                               Pinned platform JSON Schema (see platform-pin.json)
docs/                                  PRD, spike, staging integration runbooks
```

**Secrets:** Relay secret lives in `%AppData%\EventPlatform\PrintRelay\settings.json` (encrypted at rest is post-MVP). Never log secret or full setup code. Diagnostics export excludes secret (PRD §9.3).

---

## 4. Cross-repo contracts

| Contract | Source | Validation |
|---|---|---|
| Print relay HTTP API | Parent PRD §14.4 | Four `/api/print-queue` endpoints |
| Pending job JSON | `schemas/pending-job.response.json` | Pinned from platform; xUnit + JsonSchema.Net |
| Setup code `v: 1` | `schemas/desk-setup-code.v1.json` | Decode after stripping `DESK-` prefix |
| `badge_document` shape | `schemas/badge-render-input.json` | Validate for diagnostics; **do not render** |

Breaking changes require parent PRD §26 process and coordinated release. Bump `schemas/platform-pin.json` when platform schemas change.

---

## 5. Hard runtime rules

- Print **only** `badge_html` from the platform — never implement badge layout in C#
- Poll interval **1000 ms** — not operator-configurable (match Node relay)
- One relay instance per desk secret — no multi-desk from one process
- Fail jobs with plain-English `message` (max 500 chars) — no stack traces to API
- Operator UI never shows event ID, relay secret, or raw API URL
- `Authorization: Bearer relay_…` on every poll/complete/fail request

---

## 6. Testing

| Layer | Where it runs |
|---|---|
| Setup code, API parsing, backoff, schema contracts | macOS/Linux — `dotnet test` |
| WebView2 print, tray UI, MSI install | Windows VM or hardware |
| Staging E2E | Documented manual run — `docs/STAGING_INTEGRATION.md` |

Definition of done: new Core logic → xUnit; Windows UI → manual checklist in story acceptance.

---

## 7. Development environments

- **macOS:** Core development + `dotnet publish -r win-x64` cross-compile
- **Windows VM:** WebView2 print iteration (Parallels/UTM)
- **Physical Windows laptop:** Gate 3 + M4 sign-off

---

## 8. Version compatibility

Each Windows release notes must state **minimum platform version** (e.g. requires `badge_html` — platform ≥ 0.9.0). Enforced in README and GitHub release body, not runtime blocking in MVP unless `badge_html` absent.
