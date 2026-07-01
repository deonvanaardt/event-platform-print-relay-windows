# Decision log

Chronological record of **implementation-time** decisions for the Windows print relay.

**Newest entries at the top.** Keep each entry short (5–15 lines).

---

## What goes here vs elsewhere

| Question type | Document |
|---------------|----------|
| Windows app product requirements | [`docs/PRINT_RELAY_WINDOWS_PRD.md`](docs/PRINT_RELAY_WINDOWS_PRD.md) |
| Print queue API contract | Parent PRD §14.4, §26 + platform `printrelay/INTEGRATION.md` |
| Which tools and libraries | [`Tech_Stack_Decision_Record.md`](Tech_Stack_Decision_Record.md) |
| Phase order, sprint scope | [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md), [`SPRINT.md`](SPRINT.md) |
| How we chose X while coding, spikes, rejected alternatives | **This file** |

---

## Entry template

```markdown
## YYYY-MM-DD — Short title

**Status:** accepted | superseded | deprecated  
**Context:** What problem or ambiguity triggered this?  
**Decision:** What we did.  
**Alternatives considered:** What we rejected and why.  
**Consequences:** Files, tests, release notes.
```

---

## Log

<!-- Add entries above this line, newest first. -->

## 2026-07-01 — Setup wizard in WinForms App; validation in Core

**Status:** accepted  
**Context:** W-01-S05 needs a two-step first-run wizard (paste `DESK-` code, select printer) without mixing business logic into WinForms code-behind. Tray and poll-on-launch ship in later stories.  
**Decision:** New `EventPlatform.PrintRelay.App` (WinForms, `net8.0-windows`) with single-form two-panel `SetupWizardForm`. `SetupCodeValidation` in Core maps decode/API errors to PRD §5.2 operator messages. `RelaySettingsExtensions.IsComplete` gates wizard skip. S05 exits immediately when settings are complete; S06 replaces that branch with `PrintRelayPollLoop` + print path.  
**Alternatives considered:** WPF wizard — rejected to match Spike WinForms stack and avoid WebView2 WPF reference noise. Poll loop in S05 — rejected to keep story scope to wizard + persistence only.  
**Consequences:** `src/EventPlatform.PrintRelay.App/`, `SetupCodeValidation.cs`, `RelaySettingsExtensions.cs`, CI `windows-build` job builds App; xUnit validation tests on macOS/Linux CI.

## 2026-07-01 — Poll loop backoff vs auth errors

**Status:** accepted  
**Context:** PRD §8.1 distinguishes API connectivity failure (exponential backoff) from other poll failures. Tray UI (S07) needs distinct states for amber backoff vs red auth.  
**Decision:** `PrintRelayPollLoop` applies `PollBackoff` only for `HttpRequestException`, request timeouts, and HTTP 5xx on pending poll. HTTP 401/403 use the normal 1000 ms interval and signal `PrintRelayPollConnectionState.AuthError`. Other `PrintRelayApiException` (e.g. malformed 200) also use the normal interval so the loop never exits.  
**Alternatives considered:** Backoff on all poll HTTP errors — rejected because invalid secret would delay operator feedback.  
**Consequences:** `PrintRelayPollLoop.cs`, `PrintRelayPollConnectionState.cs`; xUnit coverage in `PrintRelayPollLoopTests.cs`.

## 2026-07-01 — Governance scaffold mirrors platform repo

**Status:** accepted  
**Context:** Spike passed on hardware; Windows repo needed the same planning discipline as `event-management-platform` before M1 implementation.  
**Decision:** Add `BACKLOG.md`, `SPRINT.md`, `IMPLEMENTATION_PLAN.md`, `CHANGELOG.md`, `Tech_Stack_Decision_Record.md`, `INTEGRATION.md`, `.cursor/rules/*`, pinned `schemas/` from platform E-05-S08, and pre-commit branch protection. Story IDs use `W-01-Sxx` prefix.  
**Alternatives considered:** Track Windows work only in platform `printrelay/` — rejected (separate deployable, separate CI, agents need local scope).  
**Consequences:** Agents read `SPRINT.md` before coding; contract bumps update `schemas/platform-pin.json` + `DECISIONS.md`.

## 2026-07-01 — Physical print via WebView2 PDF + Pdfium spooler (spike)

**Status:** accepted (spike); review before M1 production path  
**Context:** Gate 3 on venue hardware: some badge printer drivers ignore WebView2 `PrintAsync` HTML paths or show dialogs; CR80 stock validation needed reliable silent output.  
**Decision:** Spike renders HTML to PDF with `CoreWebView2.PrintToPdfAsync` at explicit mm page size, then spools PDF via `PdfiumPrinter` to the named printer. `Microsoft Print to PDF` writes directly for dev verification.  
**Alternatives considered:** `CoreWebView2.PrintAsync` only (PRD §8.2 primary path) — deferred until CR80 production sign-off; QuestPDF/PdfSharp layout — rejected (banned: second renderer).  
**Consequences:** `WebView2SilentPrinter.cs`, `PdfSpooler.cs`, Spike-only deps; M1 must validate CR80 `badge_html` from staging and document final print path in Tech Stack §3.

## 2026-06-30 — A5 fixture for office-paper spike sign-off

**Status:** accepted (spike only)  
**Context:** CR80 badge stock not always available during early spike; office A5 paper validates silent print and dimension handling.  
**Decision:** Spike default fixture uses A5 `@page` dimensions; retain `test-badge-cr80.html` for badge-stock validation before M4.  
**Alternatives considered:** Block spike until CR80 stock — rejected (delays Gate 3).  
**Consequences:** `docs/SPIKE.md`, `RelayConstants` A5 helpers; production jobs use server HTML with CR80 `@page` from platform renderer.

## 2026-06-30 — Core vs Spike project split

**Status:** accepted  
**Context:** macOS/Linux CI must run unit tests; WebView2 requires Windows.  
**Decision:** `EventPlatform.PrintRelay.Core` (net8.0, no UI) holds API client, setup code, settings; `EventPlatform.PrintRelay.Spike` (net8.0-windows, WinForms + WebView2) for Gate 3 only. Future `EventPlatform.PrintRelay.App` for tray MVP.  
**Alternatives considered:** Single Windows project — rejected (no cross-platform CI for business logic).  
**Consequences:** Solution layout in `IMPLEMENTATION_PLAN.md`; Spike archived after App owns print path.
