# Staging integration — print `badge_html` end-to-end

Runbook for **W-01-S06** sign-off: Windows relay against Event Platform **staging** with real queued jobs.

## Prerequisites

- Platform staging deploy includes `badge_html` on `GET /api/print-queue/pending` (E-05-S06)
- Print desk created in staging admin; **Copy setup code** used (E-05-S07)
- Windows machine with WebView2 runtime and target printer
- Relay build with setup wizard + poll loop + WebView2 print path (W-01-S04–S06) — or Spike `print-html` with saved `badge_html` for interim layout checks

## Steps

1. **Enqueue a job** — check in a delegate from staging check-in PWA (or POST print-queue via API) for the desk’s event.
2. **Paste setup code** in Windows relay wizard (or configure Spike with decoded secret + api_url).
3. **Select printer** — use physical badge printer or Microsoft Print to PDF for layout-only checks.
4. **Observe poll** — relay receives job within 1000 ms; prints `badge_html` silently.
5. **Complete** — `POST /api/print-queue/{id}/complete` returns 200; job status `printed` in platform admin.
6. **Verify output** — delegate name and QR visible; CR80 dimensions match admin preview.

## Failure cases to verify

| Condition | Expected behaviour |
|---|---|
| Missing `badge_html` on job | Fail job with plain-English message; no client render |
| Invalid / revoked secret | Wizard error: contact organiser |
| API unreachable | Amber tray / reconnecting message |
| Printer offline | Fail job; relay continues polling |

## Record results

Log date, platform version/commit, Windows app version, printer model, and pass/fail in the PR or story comment when closing W-01-S06.

## Related

- Platform smoke: `pnpm smoke:print-queue` (from event-management-platform)
- Contracts: `schemas/pending-job.response.json`, `INTEGRATION.md`
