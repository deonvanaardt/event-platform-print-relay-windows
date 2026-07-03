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

# Sprint 2 — M3 MSI + release engineering

**Dates:** TBD  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 3 — M3](IMPLEMENTATION_PLAN.md#phase-3--m3-release-engineering)  
**Spec:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §4 · `INTEGRATION.md`

## Goal

Unsigned `.msi` + release CI (HKCU auto-start, GitHub Releases). SignPath signing deferred to **W-01-S11** after OSS approval.

## In scope (Sprint 2)

- [ ] **W-01-S09** — MSI + release CI (unsigned) (M3)

## Stretch (if time remains)

_(none)_

## In progress

- **W-01-S09** — MSI + release CI (unsigned)

## Done

_(none yet)_

## Out of scope this sprint

- SignPath OSS signing CI (W-01-S11 — after SignPath approval)
- Physical sign-off matrix Win 10 + 11 (W-01-S10 — M4)
- Platform admin MSI URL (E-05-S09 — after W-01-S11 signed release)

## Blockers / notes

- Gate 4: SignPath OSS registration (parallel; does not block W-01-S09 unsigned MSI)
- **2026-07-03:** W-01-S09 implementation landed — awaiting Windows MSI acceptance per `docs/INSTALLER.md` checklist
