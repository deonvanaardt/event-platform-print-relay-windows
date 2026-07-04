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

Tray menu → **Copy diagnostics** — JSON for support (hostname, connection state, last job IDs; **no secrets**). Paste into a ticket or chat when asking for help.

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

## Related

- Platform smoke: `pnpm smoke:print-queue` (from event-management-platform)
- Contracts: `schemas/pending-job.response.json`, `INTEGRATION.md`
