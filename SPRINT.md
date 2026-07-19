# Sprint 1 — M1 staging integration — **CLOSED**

**Dates:** 2026-07-01 → 2026-07-03  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 1 — M1](IMPLEMENTATION_PLAN.md#phase-1--m1-staging-integration) · [Phase 2 — M2 tray](IMPLEMENTATION_PLAN.md#phase-2--m2-tray-app)  
**Spec:** `docs/PRINT_RELAY_WINDOWS_PRD.md` · `INTEGRATION.md`

## Goal

After Gate 3 spike sign-off, ship contract validation in CI and the first end-to-end path: decode setup code → poll staging → print `badge_html` → complete. M2 tray + diagnostics landed early for staging test visibility.

**Exit:** Staging E2E smoke test passed on physical Windows hardware (2026-07-03). See `docs/STAGING_INTEGRATION.md`.

## In scope (Sprint 1)

- [x] **W-01-S03** — JSON Schema pinning + contract tests
- [x] **W-01-S04** — Poll loop + job lifecycle
- [x] **W-01-S05** — Setup wizard (M1)
- [x] **W-01-S06** — Print `badge_html` from staging
- [x] **W-01-S07** — System tray UI (M2 early)
- [x] **W-01-S08** — Settings + diagnostics (M2 early)

## Stretch (if time remains)

_(none)_

## Recommended build order

1. **W-01-S03** — Pin schemas; add xUnit contract tests; extend CI
2. **W-01-S04** — Extract poll loop from Spike into Core (or new `App` project shell)
3. **W-01-S05** — WinForms/WPF setup wizard wired to Core
4. **W-01-S06** — Staging integration: real `badge_html` through `WebView2SilentPrinter` (CR80 dimensions)
5. **W-01-S07–S08** — Tray, Status panel, diagnostics export, JSON Lines log

## In progress

_(none — update this when you start a story)_

## Done

- **W-01-S01** — Repo scaffold + Core library
- **W-01-S02** — WebView2 silent print spike (Gate 3 passed)
- **W-01-S03** — JSON Schema pinning + contract tests
- **W-01-S04** — Poll loop + job lifecycle
- **W-01-S05** — Setup wizard (M1)
- **W-01-S06** — Print `badge_html` from staging
- **W-01-S07** — System tray UI
- **W-01-S08** — Settings + diagnostics

## Out of scope this sprint

- MSI packaging and code signing (W-01-S09)
- Platform admin copy changes (E-05-S09 — platform repo)

## Blockers / notes

- **2026-07-03:** Staging E2E smoke test **passed** on new physical Windows box (setup → poll → print `badge_html` → job `printed` on platform).
- Gate 3 passed on physical Windows laptop (A5 sign-off; CR80 fixture retained)
- Spike uses `PrintToPdf` + `PdfSpooler` for some drivers — production path must use CR80 dimensions per PRD §8.2 (see `DECISIONS.md`)

---

# Sprint 2 — M3 MSI + release engineering — **CLOSED**

**Dates:** 2026-07-03 → 2026-07-04  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 3 — M3](IMPLEMENTATION_PLAN.md#phase-3--m3-release-engineering)  
**Spec:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §4 · `INTEGRATION.md`

## Goal

Unsigned `.msi` + release CI (HKCU auto-start, GitHub Releases). SignPath signing deferred to **W-01-S11** after OSS approval.

## In scope (Sprint 2)

- [x] **W-01-S09** — MSI + release CI (unsigned) (M3)

## Stretch (if time remains)

_(none)_

## In progress

_(none)_

## Done

- **W-01-S09** — MSI + release CI (unsigned): WiX installer, finish UI + launch-on-exit, HKCU auto-start, `release.yml`, Windows acceptance per `docs/INSTALLER.md` (2026-07-04)

## Out of scope this sprint

- SignPath OSS signing CI (W-01-S11 — after SignPath approval)
- Physical sign-off matrix Win 10 + 11 (W-01-S10 — M4)
- Platform admin MSI URL (E-05-S09 — after W-01-S11 signed release)

## Blockers / notes

- Gate 4: SignPath OSS registration (parallel; does not block W-01-S09 unsigned MSI)
- **2026-07-04:** W-01-S09 Windows acceptance **passed** — MSI install, finish UI, launch-on-exit, smoke test; MSI backup in `releases/msi/`

---

# Sprint 3 — M3 SignPath signing + M4 prep

**Dates:** TBD  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 3 — M3](IMPLEMENTATION_PLAN.md#phase-3--m3-release-engineering) · [Phase 4 — M4](IMPLEMENTATION_PLAN.md#phase-4--m4-go-live)  
**Spec:** `docs/SIGNPATH.md` · `docs/PRINT_RELAY_WINDOWS_PRD.md` §4.2

## Goal

Customer-ready **signed** `.msi` on GitHub Releases via SignPath OSS. Physical Win 10/11 sign-off when signing is live.

## In scope (Sprint 3)

- [ ] **W-01-S11** — SignPath OSS signing CI (M3) — CI wired; **blocked** — SignPath OSS declined 2026-07-18 (reputation); reapply or paid signing + signed `v0.4.0` verify

## Stretch (if time remains)

- [ ] **W-01-S10** — Physical sign-off Win 10 + 11 (M4) — after signed MSI available

## In progress

- **W-01-S11** — SignPath OSS signing CI: CI + docs done; **blocked** on signing provider (SignPath declined 2026-07-18 — reapply later or choose paid signing)

## Done

_(none yet — mark W-01-S11 Done after signed release + Windows acceptance)_

## Out of scope this sprint

- Platform admin MSI URL (E-05-S09 — platform repo; after signed release URL is stable)

## Blockers / notes

- **SignPath OSS declined 2026-07-18** — insufficient external reputation; not a code/policy rejection. **Paid signing deferred** until first paying customer (Certum OSS cloud ~$50–58/yr; sole trader). Reapply SignPath when visibility grows (`docs/SIGNPATH.md`).
- CI falls back to unsigned prerelease when `SIGNPATH_API_TOKEN` is not set
- Target first signed release: tag `v0.4.0` after SignPath reapproval **or** paid signing CI is wired
- **Sprint 4** (Kiosa brand icons) runs **in parallel** — not blocked on signing

---

# Sprint 4 — Kiosa brand icons (FR-001)

**Dates:** 2026-07-18 → TBD  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 2 — M2 tray polish](IMPLEMENTATION_PLAN.md#phase-2--m2-tray-app) (parallel to Sprint 3 signing)  
**Story:** **W-01-S12** — Kiosa brand icons (tray + exe + Start Menu)  
**Spec:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §7.1 · `kiosa-marketing/brand-pack/Kiosa_Brand_Pack.md`  
**Plan:** [`docs/plans/sprint-4-kiosa-brand-icons.md`](docs/plans/sprint-4-kiosa-brand-icons.md)

## Goal

Replace placeholder `SystemIcons` with the Kiosa icon from `kiosa-marketing/brand-pack`. Tray shows the Kiosa mark with coloured status-dot overlays (green / amber / red per PRD §7.1). Executable, WinForms title bars, and Start Menu shortcut use the same branded icon.

**Naming unchanged:** window and installer text stay "Event Platform Print Relay" / "Print Relay"; only visuals switch to Kiosa.

## In scope (Sprint 4)

- [ ] **W-01-S12** — Kiosa brand icons (tray + exe + Start Menu)

### Session 1 — Assets + base icon wiring (Mac agent)

- [ ] Copy SVGs from `kiosa-marketing/brand-pack/` into `src/EventPlatform.PrintRelay.App/Assets/brand/`
- [ ] Add `scripts/generate-app-icons.sh`; generate and commit `app.ico` (16, 32, 48, 256) + `tray/base-32.png`
- [ ] Set `<ApplicationIcon>` in `EventPlatform.PrintRelay.App.csproj`
- [ ] Add `RelayAppIcons.cs`; replace `SystemIcons` in `TrayApplicationContext`
- [ ] Set `Form.Icon` on Setup, Status, and Settings forms
- [ ] Log decision in `DECISIONS.md` (asset source, overlay strategy, committed ICO)

**Windows verify (one step per operator reply):** pull → publish → confirm tray, `.exe` Properties, and form title-bar icons show Kiosa.

### Session 2 — Tray state overlays (Mac agent)

- [ ] Draw status dot on Kiosa icon: green (connected), amber (reconnecting), red (error)
- [ ] Map `RelayTrayIconState` to PRD §7.1 colours in `RelayAppIcons.CreateTrayIcon`
- [ ] If 16×16 accent is unreadable, fall back to monochrome base per brand pack §3

**Windows verify:** force reconnect → amber; error state → red; normal → green; check 16×16 in tray overflow (`^`).

### Session 3 — MSI + closure (Mac docs + Windows MSI)

- [ ] Add Start Menu / Add/Remove Programs icon check to `docs/INSTALLER.md`
- [ ] Windows: rebuild MSI, install, confirm Start Menu shortcut icon; upgrade over existing install
- [ ] Mark W-01-S12 Done; update `CHANGELOG.md`

## Stretch (if time remains)

- [ ] Setup wizard header with `kiosa-logo-primary.svg` lockup (min 120px wide per brand pack)
- [ ] `SetupRequired` grey tray state (PRD §7.1 row — enum not implemented today)

## In progress

_(none — update when Session 1 starts)_

## Done

_(none yet)_

## Out of scope this sprint

- WiX installer banner/dialog custom artwork (`WixUI_Minimal` stays generic)
- `--about` dialog custom logo (still `MessageBoxIcon.Information`)
- Product rename to "Kiosa Print Relay" (text stays Event Platform)
- SignPath signing (Sprint 3 / W-01-S11)

## Blockers / notes

- **Asset source:** `kiosa-marketing/brand-pack/kiosa-logo-icon.svg` (primary); regenerate ICO/PNG in-repo — brand pack §8 open item
- **Tray approach:** Kiosa icon + coloured status dot overlay (not separate icon files per state)
- **CI impact:** none — Core tests unchanged; `release.yml` picks up new exe icon on next tag automatically
- **Handoff:** Mac agent pushes after each session; Windows operator verifies one step at a time per `windows-operator-steps.mdc`

---

# Sprint 5 — BUG-003 dynamic page size (W-01-S13) — **CLOSED**

**Dates:** 2026-07-19 → 2026-07-19  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 1 — M1](IMPLEMENTATION_PLAN.md#phase-1--m1-staging-integration) print-path fix (post-ship)  
**Bug:** [BUG-003](BUGS.md#bug-003--relay-walk-in-badge-prints-smaller-than-designer-test-print-hardcoded-page-size) · platform **BUG-011**  
**Story:** **W-01-S13** — Dynamic badge page size from `badge_html` / `badge_document`  
**Plan:** [`docs/plans/sprint-5-bug-003-dynamic-page-size.md`](docs/plans/sprint-5-bug-003-dynamic-page-size.md)

## Goal

Fix walk-in badges printing **smaller** than designer test prints by replacing hardcoded CR80 page size in `WebView2SilentPrinter` with per-job dimensions resolved from server `badge_html` (`@page` mm CSS), with `badge_document` format fields as fallback.

**Exit:** Windows physical compare passed for A6 Landscape, A5 Portrait, and A5 Landscape on print-test PC (2026-07-19). CR80 physical N/A — test printer cannot print CR80 stock; resolver + log fields verified for non-CR80 formats. BUG-003 resolved.

## In scope (Sprint 5)

- [x] **W-01-S13** — Dynamic badge page size (BUG-003)

### Session 1 — Core resolver + unit tests (Mac agent)

- [x] Add `BadgePageDimensions` + `BadgePageDimensionResolver` in Core
- [x] Parse `@page { size: Wmm Hmm; }` from `badge_html`; fallback `badge_document` format; fallback CR80
- [x] xUnit tests (macOS CI) — no App/WebView2 changes

**Windows verify:** none (Core-only).

### Session 2 — App print path wiring (Mac agent)

- [x] `WebView2SilentPrinter` accepts resolved dimensions (mm → inches + viewport)
- [x] `BadgeHtmlPrintJobProcessor` calls resolver and passes dimensions
- [x] Log `page_width_mm`, `page_height_mm`, `page_size_source` on print jobs
- [x] Log decision in `DECISIONS.md`

**Windows verify (one step per reply):** pull → publish → Print test badge (CR80) + staging walk-in on CR80 event; compare to designer test print.

### Session 3 — Multi-format fixtures + Spike parity + staging doc (Mac agent)

- [x] Add `test-badge-a6-landscape.html` fixture (148 × 105 mm)
- [x] Align Spike `print-html` with resolver (regression CLI)
- [x] Extend resolver tests from `schemas/fixtures/pending-response.valid.json`
- [x] Update `docs/STAGING_INTEGRATION.md` dimension sign-off steps

**Windows verify:** none until Session 4.

### Session 4 — Physical sign-off + closure (Mac docs + Windows operator)

- [x] Windows: A6 Landscape, A5 Portrait, A5 Landscape — walk-in matches designer test size (print-test PC, app 0.4.1, 2026-07-19)
- [x] CR80 physical — N/A (printer cannot print CR80 stock); non-CR80 sign-off satisfies W-01-S13 minimum
- [x] Mark W-01-S13 Done; resolve BUG-003; update `CHANGELOG.md`

## Stretch (if time remains)

- [ ] Tray **Print test badge** menu picks format from last job's resolved size (today: CR80 fixture only)

## In progress

_(none)_

## Done

- **W-01-S13** — Dynamic badge page size (BUG-003): Core resolver, App wiring, Spike parity, physical sign-off A6/A5 on print-test PC (2026-07-19)

## Out of scope this sprint

- Client-side badge layout from `badge_document` JSON
- Platform Node relay fix for BUG-011 (separate repo)
- MSI / release tag (app fix ships on next publish)
- SignPath signing (Sprint 3)

## Blockers / notes

- **2026-07-19:** Physical sign-off on print-test PC — A6 Landscape, A5 Portrait, A5 Landscape **pass**; `relay.log` shows correct `page_width_mm` / `page_size_source: html`. CR80 not tested on hardware (printer cannot print CR80 stock).
- Runs **in parallel** with Sprint 3 (signing) and Sprint 4 (Kiosa icons)
- Should complete **before** W-01-S10 physical sign-off matrix if print size was blocking confidence
- Full plan: [`docs/plans/sprint-5-bug-003-dynamic-page-size.md`](docs/plans/sprint-5-bug-003-dynamic-page-size.md)
