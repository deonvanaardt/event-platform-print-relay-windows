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

## 2026-07-04 — MIT license + code signing policy for SignPath OSS eligibility

**Status:** accepted  
**Context:** SignPath Foundation terms require an OSI-approved license, no proprietary project components, and a published code signing policy with SignPath attribution. README stated “Proprietary” with no root `LICENSE`; release notes for v0.3.1 did not mention SignPath on the Releases page.  
**Decision:** Add root `LICENSE` (MIT) for this repository; `docs/CODE_SIGNING_POLICY.md` with required SignPath attribution, roles, and privacy statement; link both from README **Download** / **License**. Extend `release.yml` to write a **Code signing** section on every tag release body. MSI installer keeps separate `license.rtf` for venue deployment terms.  
**Alternatives considered:** Keep proprietary README and reply if asked — rejected (SignPath terms require OSI license). Retro-edit all past releases via CI — rejected (only future tags automated; v0.3.1 is one manual edit).  
**Consequences:** `LICENSE`, `docs/CODE_SIGNING_POLICY.md`, `docs/SIGNPATH_OSS_APPROVAL.md`, `release.yml`, README; parent SaaS repo unchanged.

## 2026-07-04 — Installer finish UI with launch-on-exit

**Status:** accepted  
**Context:** MSI had no wizard UI — install ended on a bare progress/close flow with no success confirmation and no way to open the app immediately after install.  
**Decision:** Add `WixUI_Minimal` via `WixToolset.UI.wixext`; success text and checked **Start Print Relay now** on `ExitDialog`; `WixUnelevatedShellExec` custom action (`WixToolset.Util.wixext`) on Finish so the tray app starts as the installing user, not elevated.  
**Alternatives considered:** Custom `ExitDialog` with a separate Start button (fork WiX UI source — rejected for MVP scope); `WixShellExec` (rejected — per-machine install runs elevated).  
**Consequences:** `Package.wxs`, `license.rtf`, UI + Util wixext packages; `docs/INSTALLER.md` acceptance extended.

## 2026-07-03 — WebView2 user data under LocalAppData for MSI install

**Status:** accepted  
**Context:** MSI installs to Program Files. WebView2 `CreateAsync()` without `userDataFolder` defaults to a folder next to the exe, which is not writable for normal users — startup fails with a misleading “install WebView2 Runtime” dialog even when Evergreen is present. Dev `artifacts\app` runs worked because the folder was user-writable.  
**Decision:** `WebView2Paths.UserDataFolder` → `%LocalAppData%\EventPlatform\PrintRelay\WebView2\`; pass to `CoreWebView2Environment.CreateAsync`. Only prompt for WebView2 download when `GetAvailableBrowserVersionString()` returns null.  
**Alternatives considered:** Install to per-user LocalAppData — rejected (PRD Program Files path). Run as admin — rejected (venue UX).  
**Consequences:** `App/Printing/WebView2Paths.cs`, `WebView2SilentPrinter.cs`, `TrayApplicationContext.cs`.

## 2026-07-03 — SignPath OSS + unsigned MSI first (W-01-S09 / W-01-S11 split)

**Status:** accepted  
**Context:** PRD requires Authenticode before customer distribution; purchasing and storing a `.pfx` in GitHub secrets is a vendor dependency SignPath OSS avoids. SignPath approval takes days and must not block installer CI.  
**Decision:** **W-01-S09** ships WiX MSI + unsigned `release.yml` → GitHub Releases (prerelease). **W-01-S11** adds `signpath/github-action-submit-signing-request` after OSS approval. Secrets are SignPath API tokens only — never `WINDOWS_CODE_SIGNING_CERTIFICATE` / `.pfx`. MSI harvest uses **folder publish** (`PublishSingleFile=false`), not single-file publish.  
**Alternatives considered:** Self-managed Authenticode + `signtool` in CI — rejected (operational burden). Block all MSI work until SignPath approved — rejected (delays staging IT testing).  
**Consequences:** `installer/`, `.github/workflows/release.yml`, `docs/INSTALLER.md`, `docs/SIGNPATH.md`; governance docs name SignPath explicitly.

## 2026-07-03 — WiX Toolset 5 SDK for MSI (W-01-S09)

**Status:** accepted  
**Context:** Backlog allows WiX or equivalent; no installer existed. Need Program Files install, Start Menu, HKCU Run key, upgrade path, and heat harvest of self-contained publish folder.  
**Decision:** `installer/EventPlatform.PrintRelay.Installer` using `WixToolset.Sdk/5.0.2` + `WixToolset.Heat` with `HarvestDirectory` bound to `artifacts/publish/`. Fixed `UpgradeCode`; auto-start and shortcut as explicit components; AppData settings untouched by MSI.  
**Alternatives considered:** Advanced Installer — rejected (not in Tech Stack). Single-file publish inside MSI — rejected (Pdfium/WebView2 native layout).  
**Consequences:** `installer/EventPlatform.PrintRelay.Installer.wixproj`, `Package.wxs`, solution entry; Tech Stack §1 pins WiX 5.

## 2026-07-02 — In-place log truncation (disk cap)

**Status:** accepted  
**Context:** `relay.log` and `startup.log` append forever; a busy desk polling every 1 s can grow tens of MB per day on venue PCs with limited disk.  
**Decision:** `RelayLogRetention.TruncateIfOversized` wipes a log file in place when size reaches `RelayConstants.MaxRelayLogBytes` (5 MB) or `MaxStartupLogBytes` (256 KB). `RelayFileLogger` checks on construction and before each write; writes a single JSON `"Log truncated due to size limit."` line after wipe. No archived `relay.log.1` files — lowest disk use; recent state remains in Status panel.  
**Alternatives considered:** Rotating archives — rejected (user chose truncate). Serilog rolling file — rejected (Tech Stack: custom logging only). Operator-configurable limits — rejected (matches fixed poll interval).  
**Consequences:** `Core/Logging/RelayLogRetention.cs`, `RelayConstants` size caps, xUnit tests under `tests/.../Logging/`.

## 2026-07-02 — Pdfium native layout bridge (bblanchon vs PdfiumPrinter)

**Status:** accepted  
**Context:** `Test-Path …\runtimes\win-x64\native\pdfium.dll` returned false after correct `-r win-x64` publish; print still failed. `bblanchon.PDFium.Win32` copies `pdfium.dll` to `{x86,x64,arm64}/` at output root; PdfiumPrinter on .NET 8 loads `runtimes/win-{arch}/native/pdfium.dll` (known PdfiumPrinter #15).  
**Decision:** `build/PdfiumNative.targets` copies arch folders into `runtimes/win-{arch}/native/` on Build/Publish. `PdfiumNativeBootstrap` registers a fallback loader that also tries `{arch}/pdfium.dll` and root `pdfium.dll`. Bump `bblanchon.PDFium.Win32` to 151.0.7920.  
**Alternatives considered:** Fork PdfiumPrinter — rejected. Only document manual copy — rejected (fragile for operators).  
**Consequences:** App/Spike `Printing/PdfiumNativeBootstrap.cs`, `build/PdfiumNative.targets`; operator verify checks any of three paths.

## 2026-07-02 — ARM64 Windows publish must pin win-x64 RID (Pdfium native)

**Status:** accepted  
**Context:** Staging print failed in ~250 ms with generic “check printer” message. Test badge on ARM64 Windows surfaced missing `runtimes\win-arm64\native\pdfium.dll` — `dotnet publish` without `-r` on ARM hosts defaults to `win-arm64`; older PDFium package layout did not ship arm64 natives reliably.  
**Decision:** Document and enforce **`-r win-x64`** for operator publish on all Windows PCs (ARM runs x64 under emulation). Bump `bblanchon.PDFium.Win32` for native `win-arm64` when explicitly published. Post-publish check: `Test-Path …\runtimes\win-x64\native\pdfium.dll`.  
**Alternatives considered:** Require only win-arm64 on ARM laptops — rejected until venue hardware mix is known; x64 emulation is simpler for MVP.  
**Consequences:** `git-sync.mdc`, App/Spike csproj PDFium version; operator steps call out ARM64 trap.

## 2026-07-01 — Session state in Core; technical IDs behind Status toggle (S07–S08)

**Status:** accepted  
**Context:** Staging E2E tests were “flying blind” — invisible poll loop, no job IDs, PowerShell required to debug desk/host mismatches. PRD §7–§9 specifies tray + diagnostics but allows operator-safe defaults.  
**Decision:** Implement `RelaySessionState` + `IRelayActivitySink` in Core (testable on macOS CI). Poll loop emits poll/job lifecycle events. App ships `TrayApplicationContext`, **Status** panel (checklist + live feed + recent jobs), **Show technical details** toggle (off by default) for `desk_id` / `event_id` / job IDs, JSON Lines log, and **Copy diagnostics**. Secrets never in UI, logs, or clipboard export.  
**Alternatives considered:** Always-visible UUIDs in Status — rejected (PRD operator UX). Debug-only build flavor — rejected (staging needs trace in same binary). Platform health endpoint for desk_id without jobs — rejected (out of scope).  
**Consequences:** `Core/Diagnostics/`, `Core/Logging/RelayFileLogger.cs`, `App/Tray/`; poll loop constructor gains optional sink; `docs/STAGING_INTEGRATION.md` updated for Status-first testing.

## 2026-07-01 — App CR80 print path + hidden host for poll loop (S06)

**Status:** accepted  
**Context:** W-01-S06 needs end-to-end staging print of server `badge_html` after setup. Spike uses A5 for Gate 3 regression; production must default to CR80 per backlog acceptance. Tray UI ships in S07.  
**Decision:** Copy Spike `WebView2SilentPrinter` + `PdfSpooler` into App with CR80 `CreatePrintSettings` / host viewport. `BadgeHtmlPrintJobProcessor` implements `IPrintJobProcessor`; missing `badge_html` uses `PrintJobMessages.MissingBadgeHtml` (PRD §8.3). `RelayHostForm` (hidden, no taskbar) keeps the process alive while `PrintRelayPollLoop` runs on a thread-pool task. Spike unchanged as A5 regression CLI.  
**Alternatives considered:** Shared printing library project — rejected (scope). `CoreWebView2.PrintAsync` only — rejected (Gate 3 Pdfium path retained). Tray host in S06 — rejected (S07 scope).  
**Consequences:** `App/Printing/`, `App/Polling/BadgeHtmlPrintJobProcessor.cs`, `RelayHostForm.cs`, `PrintJobMessages.cs`; manual staging sign-off per `docs/STAGING_INTEGRATION.md`.

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
