---
title: Implementation Plan — Windows Print Relay
version: 1.0
date: 2026-07-01
status: active
owner: Founder
companion: docs/PRINT_RELAY_WINDOWS_PRD.md, BACKLOG.md, Tech_Stack_Decision_Record.md
---

# Implementation Plan — Windows Print Relay

Phased delivery for the signed `.msi` tray app. Does not replace `docs/PRINT_RELAY_WINDOWS_PRD.md` — operationalises it.

## Document precedence

1. Parent PRD §14.4, §26 (`event-management-platform/Event_Platform_PRD_v5.md`)
2. `docs/PRINT_RELAY_WINDOWS_PRD.md` v3.0 — Windows app requirements
3. [`Tech_Stack_Decision_Record.md`](Tech_Stack_Decision_Record.md) — tools and hard constraints
4. This plan and [`BACKLOG.md`](BACKLOG.md) — execution order
5. [`INTEGRATION.md`](INTEGRATION.md) — two-project checklist with platform repo

## Agile model

| Artifact | Location | Purpose |
|---|---|---|
| Product requirements | `docs/PRINT_RELAY_WINDOWS_PRD.md` | Operator UX, print behaviour, gates |
| Stories | `BACKLOG.md` | Sprint-ready W-01-Sxx units |
| Phases | This document | M0–M4 build order |
| Live sprint | `SPRINT.md` | Current scope |
| Shipped capabilities | `CHANGELOG.md` | What exists |
| Implementation choices | `DECISIONS.md` | Spikes, rejected alternatives |

**Cadence:** 1-week sprints aligned with platform Sprint 9+ where integration stories overlap.

**Definition of Ready**

- Acceptance criteria testable
- Platform dependency merged on staging (if any)
- No conflict with parent PRD §26 or pinned schemas

**Definition of Done**

- Acceptance criteria pass
- New Core logic has xUnit coverage (runs on macOS/Linux CI)
- Windows-only behaviour documented + verified on hardware before M4
- `SPRINT.md` and `CHANGELOG.md` updated same session

---

## Pre-coding gates (PRD §12)

| Gate | Status | Notes |
|---|---|---|
| 1. Renderer coupling (`badge_html`) | ✅ Platform E-05-S06 | Server HTML on pending |
| 2. Setup code `v: 1` frozen | ✅ Platform E-05-S07 + schemas | `DESK-` + JSON schema |
| 3. WebView2 silent print spike | ✅ W-01-S02 | A5 physical sign-off |
| 4. SignPath OSS registration | ⏳ | Apply per `docs/SIGNPATH.md`; W-01-S11 CI wired — pending approval + first signed release |
| 5. Contract verification | 🔄 W-01-S03 | Pin schemas + CI validation |
| 6. JSON Schema export | ✅ Platform E-05-S08 | Vendored in `schemas/` |

Do not build MSI or full tray polish until Gate 3 passed ✅.

---

## Phase 0 — Spike (complete)

**Goal:** Prove silent print of fixture HTML to a named printer with no dialog.

| Deliverable | Story | Exit |
|---|---|---|
| Core library (API, setup code, settings) | W-01-S01 | `dotnet test` green on ubuntu |
| WebView2 spike CLI | W-01-S02 | Physical printer sign-off |

**Exit criteria:** Gate 3 pass criteria in `docs/SPIKE.md` met on Windows hardware.

---

## Phase 1 — M1 staging integration

**Goal:** Operator can paste setup code, poll staging, print real `badge_html`, complete job.

| Deliverable | Story | Depends on |
|---|---|---|
| Schema pinning + contract tests | W-01-S03 | Platform E-05-S08 |
| Poll loop + complete/fail | W-01-S04 | W-01-S01 |
| Setup wizard | W-01-S05 | W-01-S03, staging |
| Staging E2E print | W-01-S06 | W-01-S02, W-01-S04, W-01-S05 |

**Exit criteria:** Documented staging run in `docs/STAGING_INTEGRATION.md`; job completes within 5 s of poll on staging.

**Sprint:** Sprint 1 (`SPRINT.md`).

---

## Phase 2 — M2 tray app

**Goal:** Production operator UX — tray, settings, diagnostics, printer validation.

| Deliverable | Story |
|---|---|
| System tray UI | W-01-S07 |
| Settings + diagnostics + logging | W-01-S08 |

**Exit criteria:** PRD §7 and §9 acceptance; graceful shutdown (§10).

---

## Phase 3 — M3 release engineering

**Goal:** MSI installer, auto-start, GitHub Releases artifact; SignPath signing for customer distribution.

| Deliverable | Story |
|---|---|
| MSI + unsigned release CI | W-01-S09 |
| SignPath OSS signing CI | W-01-S11 |

**Exit criteria:** IT can install unsigned MSI for staging (W-01-S09); signed MSI passes SmartScreen for customer pilots (W-01-S11); HKCU auto-start works for installing user.

---

## Phase 4 — M4 go-live

**Goal:** Venue-ready sign-off and version matrix.

| Deliverable | Story |
|---|---|
| Win 10 + 11 physical printer sign-off | W-01-S10 |
| Platform pilot (one Windows + one Node desk) | `INTEGRATION.md` Phase D |

---

## Repository layout (target)

```
src/
  EventPlatform.PrintRelay.Core/     # API, setup code, poll loop, settings (cross-platform tests)
  EventPlatform.PrintRelay.App/      # Tray app, wizard, WebView2 print (Windows only) — Phase 1–2
  EventPlatform.PrintRelay.Spike/    # Gate 3 CLI; keep until App print path proven, then archive
installer/
  EventPlatform.PrintRelay.Installer/  # WiX MSI (W-01-S09)
tests/
  EventPlatform.PrintRelay.Core.Tests/
schemas/                             # Pinned platform JSON Schema + fixtures
docs/
  PRINT_RELAY_WINDOWS_PRD.md
  SPIKE.md
  STAGING_INTEGRATION.md             # Created in W-01-S06
```

Spike project remains for regression until `EventPlatform.PrintRelay.App` owns production print path.

---

## Cross-repo coordination

See [`INTEGRATION.md`](INTEGRATION.md). Platform deploys first for contract changes; Windows MSI declares compatible platform version in release notes.
