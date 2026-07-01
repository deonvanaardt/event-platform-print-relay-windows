# Sprint 1 — M1 staging integration

**Dates:** 2026-07-01 → 2026-07-07  
**Epic:** [W-01 — Windows print relay MVP](BACKLOG.md#w-01--windows-print-relay-mvp)  
**Phase:** [Phase 1 — M1](IMPLEMENTATION_PLAN.md#phase-1--m1-staging-integration)  
**Spec:** `docs/PRINT_RELAY_WINDOWS_PRD.md` · `INTEGRATION.md`

## Goal

After Gate 3 spike sign-off, ship contract validation in CI and the first end-to-end path: decode setup code → poll staging → print `badge_html` → complete.

## In scope (Sprint 1)

- [x] **W-01-S03** — JSON Schema pinning + contract tests
- [x] **W-01-S04** — Poll loop + job lifecycle
- [x] **W-01-S05** — Setup wizard (M1)
- [ ] **W-01-S06** — Print `badge_html` from staging

## Stretch (if time remains)

- **W-01-S07** — System tray UI (start M2 early)

## Recommended build order

1. **W-01-S03** — Pin schemas; add xUnit contract tests; extend CI
2. **W-01-S04** — Extract poll loop from Spike into Core (or new `App` project shell)
3. **W-01-S05** — WinForms/WPF setup wizard wired to Core
4. **W-01-S06** — Staging integration: real `badge_html` through `WebView2SilentPrinter` (CR80 dimensions)

## In progress

_(none — update this when you start a story)_

## Done

- **W-01-S01** — Repo scaffold + Core library
- **W-01-S02** — WebView2 silent print spike (Gate 3 passed)
- **W-01-S03** — JSON Schema pinning + contract tests
- **W-01-S04** — Poll loop + job lifecycle
- **W-01-S05** — Setup wizard (M1)

## Out of scope this sprint

- MSI packaging and code signing (W-01-S09)
- Full tray UX polish (W-01-S07 — stretch only)
- Platform admin copy changes (E-05-S09 — platform repo)

## Blockers / notes

- Gate 3 passed on physical Windows laptop (A5 sign-off; CR80 fixture retained)
- Platform staging must have `badge_html` (E-05-S06) before W-01-S06 sign-off
- Spike uses `PrintToPdf` + `PdfSpooler` for some drivers — production path must use CR80 dimensions per PRD §8.2 (see `DECISIONS.md`)

---

_Rewrite at sprint start. Git history keeps prior sprints._
