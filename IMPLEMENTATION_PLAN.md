---
title: Implementation Plan — Windows Print Relay
version: 1.1
date: 2026-07-18
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
| 4. SignPath OSS registration | ❌ Declined 2026-07-18 | Reputation signals insufficient — **not** a policy/code rejection. See [Code signing strategy](#code-signing-strategy-2026-07-18) |
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
| Kiosa brand icons (tray + exe + Start Menu) | W-01-S12 (Sprint 4) |

**Exit criteria:** PRD §7 and §9 acceptance; graceful shutdown (§10).

---

## Phase 3 — M3 release engineering

**Goal:** MSI installer, auto-start, GitHub Releases artifact; **customer-ready Authenticode** when a signing provider is available.

| Deliverable | Story | Status |
|---|---|---|
| MSI + unsigned release CI | W-01-S09 | ✅ Done — unsigned `.msi` on GitHub Releases (prerelease) |
| Signed MSI + customer release | W-01-S11 | 🔄 **Blocked** — CI wired; no signing provider active (see below) |

**Exit criteria (unsigned path — met):** IT can install unsigned MSI for staging; HKCU auto-start works for installing user; `docs/INSTALLER.md` acceptance passes.

**Exit criteria (signed path — not met):** Signed MSI passes SmartScreen without *Run anyway*; platform admin MSI URL (E-05-S09) points to stable signed asset.

**Current release channel:** Unsigned GitHub Release prereleases (`v0.3.x`). Operators bypass SmartScreen via *More info → Run anyway* (`docs/INSTALLER.md`).

---

## Code signing strategy (2026-07-18)

Operationalises [`DECISIONS.md`](DECISIONS.md) entries *SignPath Foundation OSS declined* and *Defer paid signing until first paying customer*.

### Where we are

| Item | State |
|---|---|
| WiX MSI + `release.yml` unsigned build | ✅ Shipped (W-01-S09) |
| SignPath GitHub Action step in `release.yml` | ✅ Wired — runs only when SignPath secrets are set |
| SignPath OSS application | ❌ Declined 2026-07-18 (external reputation, not license/policy) |
| Paid signing purchase | ⏸ **Deferred** until first paying Event Platform customer |
| First signed release (`v0.4.0` tag) | ❌ Not shipped |
| Platform `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` (E-05-S09) | ❌ Blocked on signed release URL |

### What continues without signing

- Staging and internal venue testing on **unsigned** MSI from GitHub Releases
- MVP bug fixes, tray UX, staging E2E (`mvp-test` and follow-on work)
- Physical printer validation (W-01-S10) on unsigned builds where SmartScreen bypass is acceptable

### What stays blocked until a signed MSI exists

- Customer-facing distribution without SmartScreen warnings
- W-01-S11 story closure (signed release + Windows `Get-AuthenticodeSignature` verify)
- Platform desk instructions with public MSI download URL (E-05-S09)

### Provider paths (pick one when unblocked)

| Path | Trigger | Cost | Work required |
|---|---|---|---|
| **A — Reapply SignPath OSS** | 2+ stronger visibility signals (stars/forks, downloads, third-party mention) | Free | Reapply at signpath.io; on approval, add GitHub secrets; tag `v0.4.0`; no CI code change (already wired). Runbook: `docs/SIGNPATH.md` |
| **B — Certum Open Source (cloud)** | **First paying Event Platform customer** (chosen paid path for UK sole trader) | ~$50–58/year | Purchase Certum OSS; identity verification; sign via SimplySign (manual on Windows PC first, or automate in `release.yml` at purchase time); update `Tech_Stack_Decision_Record.md` §1 signing row; tag signed release; verify per `docs/INSTALLER.md` |
| **C — Azure Artifact Signing** | UK Ltd formed or org account available | ~$10/month | New CI integration; update Tech Stack; EU/UK org eligibility |

**Operator decision (2026-07-18):** Path **B** when customer distribution is required; pursue Path **A** in parallel when visibility grows (no cost). Do **not** buy Certum or wire `.pfx`/paid CI before the customer trigger unless decision is explicitly revised in `DECISIONS.md`.

### W-01-S11 completion checklist (any provider)

1. **Provider live** — SignPath OSS approved *or* Certum (or other) certificate issued and documented in Tech Stack.
2. **CI or manual sign** — `release.yml` produces Authenticode-signed `.msi` (deep-sign: inner `.exe`/DLLs + outer MSI).
3. **Windows verify** — `Get-AuthenticodeSignature` shows valid publisher on venue PC (`docs/INSTALLER.md`).
4. **GitHub Release** — Tag (e.g. `v0.4.0`); release **not** prerelease; signed `.msi` asset attached.
5. **Platform handoff** — Set `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` to signed asset URL (E-05-S09).
6. **Governance** — `SPRINT.md` W-01-S11 → Done; `CHANGELOG.md` promoted; `BACKLOG.md` story checked.

### Parallel work (not blocked on signing)

- Fix open bugs from Windows MVP test (`BUGS.md`)
- **W-01-S12** Kiosa brand icons — Sprint 4; assets in `kiosa-marketing/brand-pack/` (FR-001)
- Reapply to SignPath when visibility criteria met (operator action, no code)
- W-01-S10 physical sign-off on unsigned MSI if needed for print-path confidence before customer ship

**Detail:** `docs/SIGNPATH.md` (SignPath + Certum checklists), `docs/CODE_SIGNING_POLICY.md`, `docs/INSTALLER.md` (unsigned vs signed install).

---

## Phase 4 — M4 go-live

**Goal:** Venue-ready sign-off and version matrix; customer pilots after signed MSI (Phase 3 signing path complete).

| Deliverable | Story | Depends on |
|---|---|---|
| Win 10 + 11 physical printer sign-off | W-01-S10 | Print path stable; may use unsigned MSI for internal validation |
| Platform pilot (one Windows + one Node desk) | `INTEGRATION.md` Phase D | Signed MSI URL (E-05-S09) for customer-facing desk instructions |

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
