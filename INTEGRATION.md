---
title: Print Relay — Two-Project Integration Plan (Windows repo view)
version: 1.1
date: 2026-07-01
status: active
owner: Founder
related:
  - event-management-platform/Event_Platform_PRD_v5.md §14.4, §26
  - docs/PRINT_RELAY_WINDOWS_PRD.md
  - event-management-platform/printrelay/INTEGRATION.md
---

# Print Relay — Two-Project Integration Plan

How **event-platform-print-relay-windows** (this repo) integrates with **event-management-platform**.

Canonical copy also lives in the platform repo at `printrelay/INTEGRATION.md`. Update **both** when checklist status changes materially.

---

## Repositories

| Repository | Contents | Deploy target |
|---|---|---|
| **event-management-platform** | Print queue API, `renderBadgeDocument`, admin setup code UI, JSON schemas | Vercel |
| **event-platform-print-relay-windows** (this repo) | .NET tray app, WebView2 print, MSI, code signing | GitHub Releases → admin MSI link |

---

## Stable contracts (do not break without PRD §26 update)

### 1. Print relay HTTP API

Four unversioned endpoints:

- `POST /api/print-queue` (staff — not used by Windows relay)
- `GET /api/print-queue/pending`
- `POST /api/print-queue/{id}/complete`
- `POST /api/print-queue/{id}/failed` — body `{ "message"?: string }`

Auth: `Authorization: Bearer relay_…`

### 2. Pending job response

Windows app **requires** additive `badge_html`. If missing, fail the job with a plain-English error.

Pinned schema: [`schemas/pending-job.response.json`](schemas/pending-job.response.json)  
Platform pin: [`schemas/platform-pin.json`](schemas/platform-pin.json)

### 3. Desk setup code `v: 1`

```
DESK-<base64url(JSON)>
```

Decoded payload validated against [`schemas/desk-setup-code.v1.json`](schemas/desk-setup-code.v1.json).

---

## Integration checklist (ordered)

### Phase A — Platform

- [x] Parent PRD §14.4 / §26 amended (2026-06-30)
- [x] E-05-S06: `badge_html` on pending
- [x] E-05-S07: Copy setup code in admin
- [x] E-05-S08: JSON schemas committed
- [ ] E-05-S09: Print desk panel + checklist updated
- [x] Smoke test: `pnpm smoke:print-queue` on platform

### Phase B — Windows spike (this repo)

- [x] Create repository
- [x] WebView2 spike: silent fixture print (Gate 3 passed — A5 physical sign-off)
- [ ] Integrate against staging: poll → print `badge_html` → complete (W-01-S06)

### Phase C — Windows MVP (this repo)

- [x] W-01-S03–S06: Schema CI + poll loop + wizard + staging E2E
- [x] W-01-S07–S08: Tray UI + diagnostics
- [ ] W-01-S09: Signed MSI + CI release
- [ ] Set `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` in platform admin

### Phase D — Go-live

- [ ] W-01-S10: Physical printer test Windows 10 + 11
- [ ] Version matrix in both READMEs
- [ ] Pilot: one Windows desk + one Node desk for parity

---

## Release coordination

| Change type | Platform deploy | Windows MSI |
|---|---|---|
| Renderer bugfix | Yes | No |
| `badge_html` field | Yes (required) | First production build after staging |
| Setup code `v: 2` | Yes + Windows update | Yes — coordinated |
| Schema breaking change | PRD §26 | Yes + bump `platform-pin.json` |

---

## References

- Windows PRD: [`docs/PRINT_RELAY_WINDOWS_PRD.md`](docs/PRINT_RELAY_WINDOWS_PRD.md)
- Platform integration: `event-management-platform/printrelay/INTEGRATION.md`
- Backlog: [`BACKLOG.md`](BACKLOG.md)
