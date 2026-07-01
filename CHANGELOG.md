# Changelog

Cumulative record of **what has been implemented** in this repo. Helps humans and agents see what already exists without re-exploring the codebase.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
**During a sprint:** add bullets under `[Unreleased]`. **At sprint close:** rename to `X.Y.Z — YYYY-MM-DD — Sprint N title` and open a fresh `[Unreleased]`.

Story IDs link to [BACKLOG.md](BACKLOG.md). The agent maintains this file per `.cursor/rules/changelog.mdc`.

---

## [Unreleased]

### Added

- `EventPlatform.PrintRelay.App` WinForms setup wizard: paste `DESK-` code, validate via pending poll, printer dropdown, persist to `%AppData%`; skip wizard when settings complete (W-01-S05)
- `SetupCodeValidation` with PRD operator-safe error messages; `RelaySettingsExtensions.IsComplete` (W-01-S05)
- xUnit tests for setup validation, settings completeness, and settings store round-trip (W-01-S05)
- `PrintRelayPollLoop` with `IPrintJobProcessor`, sequential `created_at` job processing, complete/fail lifecycle, connectivity backoff, and per-job failure isolation (W-01-S04)
- xUnit poll loop tests: ordering, lifecycle, backoff, auth vs connectivity, crash isolation (W-01-S04)
- Pinned platform JSON Schema under `schemas/` with `platform-pin.json` commit SHA (W-01-S03)
- xUnit contract tests validate fixtures and reject invalid samples; CI runs via existing `core-tests` job (W-01-S03)

### Not yet built

- Staging E2E — print `badge_html` (W-01-S06)
- System tray UI (W-01-S07)
- Settings + diagnostics (W-01-S08)
- Signed MSI + CI release (W-01-S09)
- Physical sign-off Win 10 + 11 (W-01-S10)
- Full list: [BACKLOG.md](BACKLOG.md)

---

## 0.1.0 — 2026-07-01 — Spike — Gate 3

**Stories:** W-01-S01, W-01-S02

### Added

- `EventPlatform.PrintRelay.Core`: `PrintRelayApiClient` (pending, complete, fail), `DeskSetupCodeCodec`, `RelaySettingsStore`, `PollBackoff` (W-01-S01)
- xUnit tests for setup code round-trip, API client auth headers, backoff (W-01-S01)
- `EventPlatform.PrintRelay.Spike`: WebView2 silent print CLI (`list-printers`, `print-test`, `print-html`); CR80 + A5 fixtures (W-01-S02)
- CI: Core tests on ubuntu; Spike build + win-x64 publish on windows-latest (W-01-S01)
- Governance docs: backlog, sprint board, implementation plan, tech stack record, Cursor rules (planning pass)
