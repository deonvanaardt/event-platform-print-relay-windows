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

## 2026-07-20 — WiX dialog/banner BMP layout fix (W-01-S14)

**Status:** accepted  
**Context:** Windows MSI verify showed license left panel as blank teal (no icon) and finish/progress dialogs with text overlapping a centred logo — full-canvas `#115E59` + centred icon does not match how `WixUI_Minimal` composites `WixUIDialogBmp` / `WixUIBannerBmp`.  
**Decision:** Regenerate BMPs per stock WiX layout: dialog **493×312** = white right side for text overlay + **164×312** teal branding strip on the left with ~118px icon centred in that strip; banner **493×58** = white with ~50px icon on the **right** (progress text overlays the banner). Use `-depth 24` BMP3 output.  
**Alternatives considered:** Custom WiX dialog XML (rejected — backlog stays on `WixUI_Minimal`); full-teal dialog with smaller centred icon (rejected — icon still outside 164px strip).  
**Consequences:** `scripts/generate-app-icons.sh`, committed `wix-*.bmp`; operator must rebuild MSI and reinstall.

## 2026-07-20 — WiX installer BMP branding (W-01-S14)

**Status:** superseded — see layout fix entry above (2026-07-20)  
**Context:** Sprint 4 FR-002 required Kiosa-branded `WixUI_Minimal` banner/dialog images and version text on the finish dialog; stock WiX artwork remained after W-01-S12.  
**Decision:** Extend `scripts/generate-app-icons.sh` to emit committed 493×58 and 493×312 BMP3 files under `installer/EventPlatform.PrintRelay.Installer/Assets/brand/` — solid `#115E59` background (from `kiosa-logo-icon.svg`) with centred/resized Kiosa icon via ImageMagick; wire `WixUIBannerBmp` / `WixUIDialogBmp` in `Package.wxs`. Show `$(var.ProductVersion)` in `WIXUI_EXITDIALOGOPTIONALTEXT` on the finish dialog (no custom welcome dialog — stays within `WixUI_Minimal`).  
**Alternatives considered:** Custom WiX welcome dialog fragment for version on welcome (deferred — finish text meets backlog “welcome and/or finish”); runtime-generated BMPs on Windows CI (rejected — Mac script + committed assets matches `app.ico` pattern).  
**Consequences:** `wix-banner.bmp`, `wix-dialog.bmp`, `Package.wxs`, `docs/INSTALLER.md` W-01-S14 checklist; Windows MSI interactive install verify required before story closure.

## 2026-07-20 — Kiosa brand icons + product rename (W-01-S12)

**Status:** accepted  
**Context:** Sprint 4 FR-001 required Kiosa icons (tray, exe, forms, Start Menu) and operator-facing rename from "Event Platform" to Kiosa (ARP, Task Manager, UI strings).  
**Decision:** Copy `kiosa-logo-icon.svg` from `kiosa-marketing/brand-pack` into `Assets/brand/`; generate committed `app.ico` (16/32/48/256) and `tray/base-32.png` via `scripts/generate-app-icons.sh` (rsvg-convert + ImageMagick). Set `<ApplicationIcon>` plus `AssemblyTitle`/`Product`/`Company` metadata for Task Manager display name **Kiosa Print Relay**. `RelayAppIcons` draws PRD §7.1 status-dot overlays (green/amber/red) on a **monochrome icon-only** tray base (`kiosa-tray-icon.svg`, no amber accent per brand pack §3) — full-colour icon remains on exe and form title bars. Status dot sized ~10px on 32px base with white outline for contrast (Windows tray verify 2026-07-20). Central `RelayProductName.DisplayName` constant for App strings; WiX `Package/@Name`, `Manufacturer`, Start Menu folder/shortcut, and `ARPPRODUCTICON` updated to match. **Exe filename stays `EventPlatform.PrintRelay.exe`** (HKCU Run key and MSI upgrade path unchanged).  
**Alternatives considered:** Separate icon file per tray state (rejected — overlay on one base per sprint plan); exe rename to `Kiosa.PrintRelay.exe` (deferred — breaking for registry/scripts).  
**Consequences:** `RelayAppIcons.cs`, `RelayProductName.cs`, `Package.wxs`, operator UI strings; Windows verify Task Manager friendly name on hardware.

## 2026-07-19 — Two Windows machines: build VM vs print-test PC

**Status:** accepted  
**Context:** Operator has a Windows VM on Mac (git + .NET, no printer) and a separate physical Windows PC (printer, insufficient resources for git/build). Prior rules assumed one Windows box did pull, publish, and physical print.  
**Decision:** **Build VM** — `git pull`, `dotnet publish`, verify via `build-info.txt`, zip `artifacts\app`. **Print-test PC** — extract zip, run `.exe`, physical staging sign-off and `relay.log` only; no git or SDK. Agent handoff steps must label which machine.  
**Alternatives considered:** Install git/SDK on print-test PC — rejected (hardware constraints). Build on Mac cross-compile only — rejected; WebView2/print path still needs Windows publish.  
**Consequences:** `.cursor/rules/windows-operator-steps.mdc`, `.cursor/rules/git-sync.mdc`, `conventions.mdc`.

## 2026-07-19 — WinExe `--version` is blank in PowerShell (use build-info.txt)

**Status:** accepted  
**Context:** Operator repeatedly runs `EventPlatform.PrintRelay.exe --version` after publish and sees no output — looks like a failed build. App is `OutputType` WinExe; `Console.WriteLine` in `--version` has no attached console in PowerShell.  
**Decision:** Operator verify step uses `Get-Content build-info.txt` + `FileVersion` on the exe. Document in `git-sync.mdc` and `windows-operator-steps.mdc`. Do not treat blank `--version` as publish failure.  
**Alternatives considered:** Change to `OutputType` Exe — rejected (would flash console on normal tray launch). `AllocConsole()` for `--version` only — deferred.  
**Consequences:** Rules + README; `--version` flag kept for possible future fix.

## 2026-07-18 — Diagnostics export to file, not clipboard

**Status:** accepted (supersedes same-day “Copy diagnostics on Status panel” entry for export mechanism)  
**Context:** BUG-002 — even **Status** panel **Copy diagnostics** failed with STA errors because `ShowStatusForm()` was invoked from the NotifyIcon menu thread, so the form and its controls were created on a non-STA thread. Clipboard cannot be made reliable from that path without a full UI-thread form host.  
**Decision:** **Export diagnostics** writes `diagnostics-export.json` under `%AppData%\EventPlatform\PrintRelay\logs\`. Status panel button shows the path; operator attaches file or copies contents manually. Marshal `ShowStatusForm` / `ShowSettingsForm` via `_syncForm.BeginInvoke` so child forms are always created on the main UI thread.  
**Alternatives considered:** Clipboard after UI-thread form host — rejected after Status-button retest still failed; persistent STA worker — overkill for support export.  
**Consequences:** `RelayDiagnosticsExporter.cs`, `StatusForm.cs`, `TrayApplicationContext.cs`; PRD §9.3 clipboard wording deferred — file export meets support needs.

## 2026-07-18 — Copy diagnostics on Status panel, not tray menu

**Status:** accepted  
**Context:** BUG-002 — tray **Copy diagnostics** failed on Windows with STA/OLE errors. Multiple marshaling fixes (`BeginInvoke`, dedicated STA thread, `Control.Invoke`, `UiThreadSync`) still failed because `NotifyIcon` context-menu callbacks run on a non-STA thread where `InvokeRequired` is unreliable.  
**Decision:** Remove **Copy diagnostics** from the tray menu. Add a **Copy diagnostics** button on the **Status** form; button `Click` runs on the form UI thread where `Clipboard` works. Same JSON export via `RelayRuntime.BuildDiagnosticsJson()`.  
**Alternatives considered:** Persistent STA worker thread with message pump (WebView2 pattern) — heavier than needed for MVP; keep fighting tray marshaling — rejected after repeated Windows failures.  
**Consequences:** `StatusForm.cs`, `TrayApplicationContext.cs`; `StaClipboard.cs` removed. PRD §7 tray menu list differs slightly; diagnostics export capability unchanged. Update `docs/STAGING_INTEGRATION.md` operator path.

## 2026-07-18 — Defer paid signing until first paying customer (sole trader)

**Context:** SignPath OSS declined for reputation (not policy). Operator is a UK sole trader — Azure Artifact Signing Public Trust is org-only in the UK (individual path US/Canada). Cheapest paid path when needed: Certum Open Source Code Signing in the Cloud (~$50–58/year).
**Decision:** **Do not** purchase signing or wire paid-signing CI until the first paying Event Platform customer. Continue unsigned GitHub Release prereleases for staging and internal venue testing (SmartScreen → *More info → Run anyway*). Reapply to SignPath OSS in parallel when visibility grows (free). Trigger Certum OSS purchase + signing workflow when customer distribution is required.
**Alternatives considered:** Buy Certum now — rejected (no paying customer yet). Form UK Ltd for Azure — rejected (premature; revisit if volume warrants).
**Consequences:** W-01-S11 and E-05-S09 (platform MSI URL) remain blocked for customer pilots; unsigned `v0.3.x` path is the active release channel.

## 2026-07-18 — SignPath Foundation OSS declined (reputation); W-01-S11 blocked

**Context:** SignPath Foundation (Phillip Deng) declined the OSS application. Reason: insufficient external verification signals (GitHub stars/forks/contributors, third-party mentions, articles, sustained engagement) — not license, policy, or code quality. Repo already had MIT `LICENSE`, `CODE_SIGNING_POLICY.md`, `PRIVACY.md`, and wired `release.yml` SignPath step.
**Decision:** Record decline in `docs/SIGNPATH.md`, `SPRINT.md`, README. **Do not** wire `.pfx` or paid signing in CI until an explicit provider choice is made and `Tech_Stack_Decision_Record.md` is updated. Continue unsigned prereleases for staging. Reapply to SignPath when visibility grows, **or** adopt paid signing (Azure Artifact Signing for EU/UK org, Certum cloud OV) if customer MSI is needed sooner.
**Alternatives considered:** Argue with SignPath — rejected (they state they generally don't discuss policy). Block all MSI work — rejected (W-01-S09 unsigned path already ships).
**Consequences:** W-01-S11 remains open; first signed `v0.4.0` blocked; platform MSI URL (E-05-S09) waits on signing provider.

## 2026-07-18 — Process restart for tray reload (BUG-001, supersedes in-process loop)

**Status:** accepted  
**Context:** Windows retest of BUG-001 fix (`7d6d095`, `8c70191`): after `ExitThread()`, the tray exited but a second `Application.Run(SetupWizardForm)` in the same process did not stay open — app vanished from Task Manager. WinForms does not reliably run a fresh message loop after `ApplicationContext.ExitThread()`.  
**Decision:** On tray restart (`Reload` or `ResetSetup`), delete settings when `ResetSetup`, then spawn a new process via `Environment.ProcessPath` and `Environment.Exit(0)`. New process runs normal startup (wizard when settings incomplete, tray when complete). Remove `while (true)` in-process restart loop.  
**Alternatives considered:** Keep in-process loop and clear WM_QUIT / `ExitOnLastFormClosed` hacks — rejected (fragile). `--setup` CLI flag only — rejected (process restart alone is enough; settings delete before spawn).  
**Consequences:** `Program.RestartProcess()`; supersedes in-process loop portion of *Explicit restart reason for setup reset* entry.

## 2026-07-18 — Explicit restart reason for setup reset (BUG-001)

**Status:** superseded (restart transport — see *Process restart for tray reload*)  
**Context:** Re-run setup wizard deleted `settings.json` from `SettingsForm` then called `ExitThread()` synchronously from the Settings button handler. On Windows the tray often did not restart cleanly; settings could remain on disk or the wizard never appeared.  
**Decision:** Introduce `RelayRestartReason` (`Reload` vs `ResetSetup`). Settings UI signals intent only; `Program.RunAsync` deletes settings via `RelaySettingsStore.DeleteAsync` **after** the tray context disposes. `RequestRestart` closes child forms and defers `ExitThread()` with `BeginInvoke` on the sync form.  
**Alternatives considered:** Full process restart with `--setup` flag — rejected for this fix (heavier UX; in-process loop already designed for restart).  
**Consequences:** `RelaySettingsStore.DeleteAsync`, `RelayRestartReason.cs`, `RelaySettingsStoreTests`; startup.log lines for restart transitions.

## 2026-07-19 — Spike `print-html` uses Core dimension resolver

**Status:** accepted  
**Context:** Sprint 5 Session 3 — Spike `print-html` still hardcoded A5 page size while production App resolved dimensions per job (BUG-003 fix).  
**Decision:** Spike `print-html` calls `BadgePageDimensionResolver.Resolve(html, null)` and passes `BadgePageDimensions` to `WebView2SilentPrinter.PrintHtmlAsync`. `print-test` keeps the bundled A5 fixture for Gate 3 regression.  
**Alternatives considered:** CLI `--width-mm`/`--height-mm` overrides — deferred; auto-resolve from `@page` is sufficient for regression.  
**Consequences:** `EventPlatform.PrintRelay.Spike` `Program.cs`, `WebView2SilentPrinter.cs`; `test-badge-a6-landscape.html` in App and Spike `Fixtures/`.

## 2026-07-19 — Dynamic badge page size from `badge_html` / `badge_document` metadata

**Status:** accepted  
**Context:** BUG-003 — walk-in badges printed smaller than designer test prints because `WebView2SilentPrinter` hardcoded CR80 regardless of template `@page` CSS.  
**Decision:** `BadgePageDimensionResolver` in Core resolves mm in order: `@page` in `badge_html` → `badge_document.template.canvas_config.format.physicalWidth`/`physicalHeight` → CR80 default. App passes resolved `BadgePageDimensions` to `PrintHtmlAsync` for `PageWidth`/`PageHeight` and viewport px. `PrintJobOutcome` carries dimensions back to poll loop; `relay.log` records `page_width_mm`, `page_height_mm`, `page_size_source` on `PrintCompleted`. No layout rendering from `badge_document`.  
**Alternatives considered:** Parse dimensions only in App — rejected; keeps business logic in Core and testable on macOS CI. Log inside printer — rejected; poll loop owns job activity.  
**Consequences:** `BadgePageDimensionResolver.cs`, `WebView2SilentPrinter.cs`, `BadgeHtmlPrintJobProcessor.cs`, `PrintJobOutcome.cs`, `RelayFileLogger.cs`.

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
