# Staging integration — print `badge_html` end-to-end

Runbook for **W-01-S06** sign-off: Windows relay against Event Platform **staging** with real queued jobs.

## Prerequisites

- Platform staging deploy includes `badge_html` on `GET /api/print-queue/pending` (E-05-S06)
- Print desk created in staging admin; **Copy setup code** used (E-05-S07)
- Windows machine with WebView2 runtime and target printer
- Relay build with setup wizard, tray UI, poll loop + WebView2 print path (W-01-S04–S08)

## Steps

1. **Build and run** the relay app (`EventPlatform.PrintRelay.exe`). Complete setup if prompted.
2. **Open Status** — right-click the tray icon → **Status**. Leave this window open while testing.
3. **Verify checklist** — all items should show checked except “Jobs received” until someone is checked in:
   - Setup complete
   - API reachable
   - Auth valid
   - Printer installed
4. **Optional: Test connection** — tray menu → **Test connection**. Activity feed should show `Connection test OK — 0 jobs pending`.
5. **Optional: Print test badge** — tray menu → **Print test badge** (uses CR80 fixture; no platform job required).
6. **Enqueue a job** — check in a delegate from staging check-in PWA for the **same print desk** as the setup code. Use a delegate not already checked in, or tap **Reprint badge** if already checked in.
7. **Watch activity feed** — within ~1 s you should see:
   - `Received job for registration …`
   - `Printed job …` (or a plain-English failure)
8. **Technical trace** — enable **Show technical details** in Status. Compare **Event ID** with your check-in URL (`/checkin/{eventId}/…`). **Desk ID** appears after the first job arrives.
9. **Complete** — job marked `printed` on platform; badge output matches admin preview.

## Troubleshooting with the Status panel

| What you see | Likely cause |
|---|---|
| Polling… **0 jobs pending**, auth OK | Check-in desk ≠ relay desk, or wrong API host in setup code, or check-in did not enqueue (already checked in without reprint) |
| Auth error / invalid setup code | Secret revoked or setup code copied from a different deployment URL |
| Reconnecting… | Network or platform unreachable |
| Received job → Failed (missing badge HTML) | Staging deploy missing E-05-S06 |
| Printer not found | Select printer in **Settings**; restart app to resume polling |
| No tray icon | App not running — start `EventPlatform.PrintRelay.exe` |

## Copy diagnostics

Tray → **Status** → **Export diagnostics** — saves `diagnostics-export.json` under `%AppData%\EventPlatform\PrintRelay\logs\` (hostname, connection state, last job IDs; **no secrets**). Attach the file to a support ticket or open it and copy the contents.

## Failure cases to verify

| Condition | Expected behaviour |
|---|---|
| Missing `badge_html` on job | Fail job with plain-English message; activity feed shows failure |
| Invalid / revoked secret | Auth error in checklist; wizard error on re-setup |
| API unreachable | Amber tray / Reconnecting in activity feed |
| Printer offline | Fail job; relay continues polling |

## Record results

Log date, platform version/commit, Windows app version, printer model, and pass/fail in the PR or story comment when closing W-01-S06.

| Date | App version | Result | Notes |
|------|-------------|--------|-------|
| 2026-07-03 | 0.2.0 | **Pass** | Staging E2E on new physical Windows box — setup, test print, check-in job, `printed` on platform |

## Dimension sign-off (BUG-003 / W-01-S13)

After deploying the dynamic page-size fix (W-01-S13), confirm walk-in relay prints match designer **Print test badge** physical size — not smaller.

### Prerequisites

- Relay build with `BadgePageDimensionResolver` wired (Session 2+)
- Staging events using each template format you need to support (platform badge designer options):
  - **CR80** — 85.6 × 54 mm
  - **A6 Landscape** — 148 × 105 mm
  - **A5 Portrait** — 148 × 210 mm
  - **A5 Landscape** — 210 × 148 mm
- Minimum for W-01-S13 closure: **CR80** plus **at least one** non-CR80 format (recommend **A6 Landscape**). Full sign-off: compare all four formats you use in production.
- Ruler or overlay for side-by-side comparison

### Expected `relay.log` dimensions

| Template | `page_width_mm` | `page_height_mm` | `page_size_source` |
|---|---|---|---|
| CR80 | 85.6 | 54 | `html` |
| A6 Landscape | 148 | 105 | `html` |
| A5 Portrait | 148 | 210 | `html` |
| A5 Landscape | 210 | 148 | `html` |

### Steps

1. **Build and run** the relay app. Confirm setup, printer, and polling are healthy (Status checklist all green).
2. Enable **Show technical details** in the Status panel.
3. For **each template format** below, create or use a staging event with that badge template. From badge designer, run **Print test badge**. Then check in a delegate (or reprint) so the relay prints the same template. Compare physical size: walk-in badge must match designer test print — not smaller.
   - **CR80** (85.6 × 54 mm)
   - **A6 Landscape** (148 × 105 mm)
   - **A5 Portrait** (148 × 210 mm) — optional but recommended
   - **A5 Landscape** (210 × 148 mm) — optional but recommended
4. **Log check** — open `%AppData%\EventPlatform\PrintRelay\logs\relay.log` (or **Export diagnostics**). On `PrintCompleted` lines, confirm `page_width_mm`, `page_height_mm`, and `page_size_source` match the table above for each format tested.
5. **Record results** in the table below when closing W-01-S13.

| Date | App version | CR80 | A6 Landscape | A5 Portrait | A5 Landscape | Notes |
|------|-------------|------|--------------|-------------|--------------|-------|
| | | | | | | |

## Related

- Platform smoke: `pnpm smoke:print-queue` (from event-management-platform)
- Contracts: `schemas/pending-job.response.json`, `INTEGRATION.md`
