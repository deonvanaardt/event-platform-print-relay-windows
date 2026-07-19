# Bugs

Operator- and developer-reported **defects** in the Windows print relay (tray app, MSI, Core, CI).  
Use this for things that are **broken or wrong** — not ideas for new capability (see [`FEATURE_REQUESTS.md`](FEATURE_REQUESTS.md)) and not planned sprint work (see [`BACKLOG.md`](BACKLOG.md)).

**Newest entries at the top** of the Open and Resolved sections.

---

## What goes here vs elsewhere

| Item | Document |
|---|---|
| Defect: crashes, wrong print output, installer failure, API mishandling | **This file** |
| Idea for new behaviour or UX improvement | [`FEATURE_REQUESTS.md`](FEATURE_REQUESTS.md) |
| Scoped story with acceptance criteria for a sprint | [`BACKLOG.md`](BACKLOG.md) → [`SPRINT.md`](SPRINT.md) |
| Why we implemented a fix a certain way | [`DECISIONS.md`](DECISIONS.md) |
| Shipped fix in a release | [`CHANGELOG.md`](CHANGELOG.md) |

When a bug becomes sprint work, add the story ID to the bug entry (e.g. `W-01-S12`) and link both ways.

---

## Status values

| Status | Meaning |
|---|---|
| **open** | Confirmed or reported; not fixed |
| **investigating** | Reproduced or triaged; root cause not fixed |
| **fixed** | Fix merged; move to **Resolved** with version/commit |
| **wontfix** | Accepted limitation or out of scope — explain why |
| **duplicate** | Link to the canonical bug ID |

---

## Entry template

```markdown
### BUG-NNN — Short title

**Status:** open | investigating | fixed | wontfix | duplicate  
**Reported:** YYYY-MM-DD  
**App version:** e.g. 0.4.0 (or unknown)  
**Environment:** e.g. Win 11 24H2, ARM64, USB label printer  
**Story:** (optional) W-01-Sxx when promoted to backlog  

**Summary:** One sentence — what is wrong from the operator's perspective.  

**Steps to reproduce:**
1. …
2. …

**Expected:** …  
**Actual:** …  

**Notes:** Workarounds, logs path (`%AppData%\EventPlatform\PrintRelay\logs\`), related PR/commit.  
**Duplicate of:** BUG-NNN (if applicable)
```

**Rules:** Never paste relay secrets, full setup codes, or API URLs with tokens in this file.

---

## Open

<!-- Add entries above this line, newest first. Next ID: BUG-004 -->

_(none)_

---

## Resolved

<!-- Move fixed/wontfix/duplicate entries here, newest first. Keep original ID. -->

### BUG-003 — Relay walk-in badge prints smaller than designer test print (hardcoded page size)

**Status:** fixed  
**Reported:** 2026-07-19  
**Fixed:** 2026-07-19  
**App version:** 0.4.2  
**Environment:** Windows print relay (WebView2 → PrintToPdf → Pdfium spool); print-test PC  
**Story:** W-01-S13 (Sprint 5)  
**Plan:** [`docs/plans/sprint-5-bug-003-dynamic-page-size.md`](docs/plans/sprint-5-bug-003-dynamic-page-size.md)  
**Related:** event-management-platform **BUG-011** — may still affect Node relay; Windows path fixed

**Summary:** Walk-in badges printed by the Windows relay came out **smaller** than the designer **Print test badge** for non-CR80 templates because `WebView2SilentPrinter` hardcoded CR80 page size.

**Fix:** `BadgePageDimensionResolver` in Core resolves mm from `@page` in `badge_html` → `badge_document` format fields → CR80 default. App passes resolved dimensions to `PrintToPdfAsync` and logs `page_width_mm`, `page_height_mm`, `page_size_source` per job.

**Verification (2026-07-19):** Print-test PC, app 0.4.1 build from `85d99f5`. A6 Landscape, A5 Portrait, A5 Landscape — walk-in matches designer test physical size. `relay.log` confirms 148×105, 148×210, 210×148 mm with `page_size_source: html`. CR80 physical not tested (printer cannot print CR80 stock).

---

### BUG-002 — Copy diagnostics fails with STA thread error

**Status:** fixed  
**Reported:** 2026-07-18  
**App version:** 0.4.0 (or current published build)  
**Environment:** Windows (venue PC)  
**Story:** W-01-S08 (diagnostics export)

**Summary:** Tray menu **Copy diagnostics** showed an error dialog instead of copying support JSON to the clipboard.

**Steps to reproduce:**
1. Complete setup so Print Relay is running in the tray.
2. Right-click the tray icon → **Status** → **Export diagnostics**.

**Expected:** Diagnostics JSON saved to `%AppData%\EventPlatform\PrintRelay\logs\diagnostics-export.json`; dialog shows path (per PRD §9.3 intent — support bundle without secrets).  
**Actual:** Error dialog: *“Current thread must be set to single thread apartment (STA) mode…”* when using clipboard from tray or Status (wrong-thread form host).

**Notes:** Root cause: NotifyIcon menu creates forms on non-STA thread. Fixed in `d0c35b0`: marshal Status/Settings to UI thread; export to file (no clipboard). **Windows verified 2026-07-18** on `e711e31` — Status → Export diagnostics saves JSON and shows path dialog.

---

### BUG-001 — Re-run setup wizard does not restart app or show wizard

**Status:** fixed  
**Reported:** 2026-07-18  
**App version:** 0.4.0  
**Environment:** Windows (venue PC)  
**Story:** W-01-S08 (settings — re-run setup wizard)

**Summary:** Choosing **Re-run setup wizard** in Settings does not restart the relay and open the setup wizard again as promised.

**Steps to reproduce:**
1. Complete initial setup (paste `DESK-` code, pick printer) so the tray app is running.
2. Open **Settings** from the tray menu.
3. Click **Re-run setup wizard** and confirm **Yes** on the prompt (“This clears saved settings and opens the setup wizard again…”).

**Expected:** App restarts (or returns to setup flow); setup wizard appears so the operator can paste a new desk code and printer.  
**Actual:** App does not restart; setup wizard does not appear. Operator remains on the tray with prior configuration still in effect (or app exits without wizard).

**Notes:** Fixed in `e7695b7` (`RelayRestartReason.ResetSetup`, settings delete after tray dispose, `RestartProcess()`). Foreground on restart: `fda54e9` (`SetupWizardForm.BringToForeground`). **Windows verified 2026-07-18** on `fda54e9` — wizard restarts in front. Regression: printer save restarts tray without wizard.
