# Changelog

Cumulative record of **what has been implemented** in this repo. Helps humans and agents see what already exists without re-exploring the codebase.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
**During a sprint:** add bullets under `[Unreleased]`. **At sprint close:** rename to `X.Y.Z — YYYY-MM-DD — Sprint N title` and open a fresh `[Unreleased]`.

Story IDs link to [BACKLOG.md](BACKLOG.md). The agent maintains this file per `.cursor/rules/changelog.mdc`.

---

## [Unreleased]

### Added

- Pairing code setup — 8-char Crockford code exchange via `POST /api/v1/print-desks/pair`; Core layer (`PairingCodeFormat`, `PairingExchangeClient`, `DeskSetupValidation`) and setup wizard UX with collapsible Platform URL (W-01-S15)
- Kiosa brand icons — tray status-dot overlays, exe/form icons, Start Menu and ARP icon via `app.ico` (W-01-S12, Sprint 4, FR-001)
- Product rename to **Kiosa Print Relay** — operator UI, installer metadata, Task Manager display name; exe filename unchanged for upgrade compatibility (W-01-S12)
- MSI installer branding — Kiosa WiX banner/dialog BMPs (`WixUI_Minimal` layout), product version on finish dialog (W-01-S14, Sprint 4, FR-002)

### Changed

- App version **1.1.0** — pairing code setup (primary path); `DESK-` legacy decode retained for dev laptops
- Setup wizard advanced panel — fixed Platform URL textbox height (was invisible with `AutoSize` + `Dock.Fill`)
- App version **0.4.3** — MSI upgrade path after Kiosa branding (avoids stale DLLs on same-version reinstall)

### Fixed

- Setup wizard advanced panel — Platform URL textbox invisible (`AutoSize` + `Dock.Fill`); fixed height so Vercel preview/staging URL is editable (W-01-S15)

### Not yet built

- First signed GitHub Release — blocked on signing provider (SignPath OSS declined 2026-07-18; reapply or paid signing) + Windows verify
- Physical sign-off Win 10 + 11 (W-01-S10)
- Full list: [BACKLOG.md](BACKLOG.md)

---

## 0.4.2 — 2026-07-19 — Sprint 5 — BUG-003 dynamic page size

**Stories:** W-01-S13

### Fixed

- Dynamic badge page size from `badge_html` `@page` CSS with `badge_document` format fallback — walk-in prints match designer test size for A6/A5 formats (W-01-S13, BUG-003)

### Added

- `BadgePageDimensionResolver` in Core with xUnit coverage; `relay.log` records `page_width_mm`, `page_height_mm`, `page_size_source` per print job
- Multi-format test fixtures (A6 landscape, A5 portrait/landscape); Spike `print-html` uses same resolver as production
- Operator docs: two-box Windows workflow (build VM + print-test PC); verify builds via `build-info.txt` not `--version`

---

## 0.4.1 — 2026-07-19 — MVP test fixes

**Stories:** W-01-S08 (bug fixes from venue testing)

### Fixed

- Diagnostics export saves JSON to `%AppData%\EventPlatform\PrintRelay\logs\diagnostics-export.json` from Status panel — avoids NotifyIcon STA/clipboard issues (BUG-002, W-01-S08)
- Re-run setup wizard restarts relay and opens setup flow again (`RelayRestartReason`, process restart) (BUG-001, W-01-S08)
- Setup wizard brings itself to the foreground when shown after process restart (BUG-001)

### Added

- App version `0.4.1`; GitHub Release MSI for multi-machine testing

---

## 0.4.0 — 2026-07-18 — Sprint 3 — SignPath signing CI

**Stories:** W-01-S11 (CI wired; signing blocked — SignPath OSS declined)

### Added

- SignPath OSS signing in `release.yml`: conditional `signpath/github-action-submit-signing-request`, signed MSI on tag release when secrets set, unsigned prerelease fallback (W-01-S11)
- `docs/SIGNPATH.md` operator runbook: OSS application, dashboard setup, GitHub secrets, first signed release steps (W-01-S11)
- SignPath OSS approval prep: MIT `LICENSE`, `docs/CODE_SIGNING_POLICY.md`, tag release body with SignPath attribution, `docs/SIGNPATH_OSS_APPROVAL.md` (W-01-S11)
- Signed MSI verification section in `docs/INSTALLER.md` (`Get-AuthenticodeSignature`) (W-01-S11)
- App version `0.4.0`; unsigned prerelease MSI on GitHub Releases

---

## 0.3.1 — 2026-07-04 — Sprint 2 — M3 MSI + release

**Stories:** W-01-S09

### Added

- WiX MSI installer: `Program Files\EventPlatform\PrintRelay\`, Start Menu shortcut, HKCU Run auto-start (W-01-S09)
- Installer finish UI: success message, **Start Print Relay now** checkbox (checked by default), launch on Finish via `WixUnelevatedShellExec` (W-01-S09)
- `installer/EventPlatform.PrintRelay.Installer` — WiX Toolset 5 SDK, folder-publish harvest (W-01-S09)
- Release CI: `.github/workflows/release.yml` — unsigned MSI on tag / workflow dispatch (W-01-S09)
- Installer runbook `docs/INSTALLER.md`; SignPath follow-up `docs/SIGNPATH.md` (W-01-S11 prep)
- App version `0.3.1`; MSI backup under `releases/msi/` (W-01-S09)
- WebView2 user data folder under `%LocalAppData%` for Program Files MSI install
- Governance: SignPath OSS signing path documented; W-01-S09 / W-01-S11 story split

---

## 0.2.0 — 2026-07-03 — Sprint 1 — M1 staging + M2 tray

**Stories:** W-01-S03, W-01-S04, W-01-S05, W-01-S06, W-01-S07, W-01-S08

**Sign-off:** Staging E2E smoke test passed on physical Windows hardware (check-in → poll → print `badge_html` → job `printed`).

### Added

- Automatic log truncation: `relay.log` capped at 5 MB, `startup.log` at 256 KB; in-place wipe with truncation notice in `relay.log` (W-01-S08)
- System tray UI: `NotifyIcon` states, menu (Status, printer, test print, test connection, Copy diagnostics, Settings, Quit) (W-01-S07)
- Status panel: connection checklist, live activity feed, recent jobs table, **Show technical details** toggle for desk/event/job IDs (W-01-S07)
- Settings screen: desk name, printer change, re-run setup wizard, app version (W-01-S08)
- Core diagnostics: `RelaySessionState`, `RelayActivityEvent`, `IRelayActivitySink`, poll loop activity hooks, `RelayConnectionTester` (W-01-S08)
- JSON Lines log at `%AppData%\EventPlatform\PrintRelay\logs\relay.log` (no secrets) (W-01-S08)
- Copy diagnostics JSON to clipboard; xUnit tests for session state, diagnostics redaction, poll activity (W-01-S08)
- App production print path: `WebView2SilentPrinter` (CR80 default), `PdfSpooler`, `BadgeHtmlPrintJobProcessor`, hidden `RelayHostForm`; poll loop runs after setup (W-01-S06)
- `PrintJobMessages` operator-safe failure text for missing `badge_html` and printer errors (W-01-S06)
- xUnit tests for `PrintJobMessages` (W-01-S06)
- `EventPlatform.PrintRelay.App` WinForms setup wizard: paste `DESK-` code, validate via pending poll, printer dropdown, persist to `%AppData%`; skip wizard when settings complete (W-01-S05)
- `SetupCodeValidation` with PRD operator-safe error messages; `RelaySettingsExtensions.IsComplete` (W-01-S05)
- xUnit tests for setup validation, settings completeness, and settings store round-trip (W-01-S05)
- `PrintRelayPollLoop` with `IPrintJobProcessor`, sequential `created_at` job processing, complete/fail lifecycle, connectivity backoff, and per-job failure isolation (W-01-S04)
- xUnit poll loop tests: ordering, lifecycle, backoff, auth vs connectivity, crash isolation (W-01-S04)
- Pinned platform JSON Schema under `schemas/` with `platform-pin.json` commit SHA (W-01-S03)
- xUnit contract tests validate fixtures and reject invalid samples; CI runs via existing `core-tests` job (W-01-S03)

---

## 0.1.0 — 2026-07-01 — Spike — Gate 3

**Stories:** W-01-S01, W-01-S02

### Added

- `EventPlatform.PrintRelay.Core`: `PrintRelayApiClient` (pending, complete, fail), `DeskSetupCodeCodec`, `RelaySettingsStore`, `PollBackoff` (W-01-S01)
- xUnit tests for setup code round-trip, API client auth headers, backoff (W-01-S01)
- `EventPlatform.PrintRelay.Spike`: WebView2 silent print CLI (`list-printers`, `print-test`, `print-html`); CR80 + A5 fixtures (W-01-S02)
- CI: Core tests on ubuntu; Spike build + win-x64 publish on windows-latest (W-01-S01)
- Governance docs: backlog, sprint board, implementation plan, tech stack record, Cursor rules (planning pass)
