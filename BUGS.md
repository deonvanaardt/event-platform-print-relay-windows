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

<!-- Add entries above this line, newest first. Next ID: BUG-003 -->

### BUG-002 — Copy diagnostics fails with STA thread error

**Status:** open  
**Reported:** 2026-07-18  
**App version:** 0.4.0 (or current published build)  
**Environment:** Windows (venue PC)  
**Story:** W-01-S08 (diagnostics export)

**Summary:** Tray menu **Copy diagnostics** shows an error dialog instead of copying support JSON to the clipboard.

**Steps to reproduce:**
1. Complete setup so Print Relay is running in the tray.
2. Right-click the tray icon.
3. Click **Copy diagnostics**.

**Expected:** Diagnostics JSON is copied to the clipboard; balloon tip confirms “Diagnostics copied to clipboard.” (per PRD §9.3).  
**Actual:** Error dialog: *“Current thread must be set to single thread apartment (STA) mode before OLE calls can be made. Ensure that your Main function has STAThreadAttribute marked on it.”* Nothing is copied.

**Notes:** `Program.Main` already has `[STAThread]`; likely `Clipboard.SetText` in `TrayApplicationContext.CopyDiagnostics` is invoked from a non-STA context menu callback. Compare with other tray actions that marshal to the UI thread. Logs: `%AppData%\EventPlatform\PrintRelay\logs\`. Workaround: open **Status** and copy details manually, or read `relay.log` for support.

### BUG-001 — Re-run setup wizard does not restart app or show wizard

**Status:** open  
**Reported:** 2026-07-18  
**App version:** 0.4.0 (or current published build)  
**Environment:** Windows (venue PC)  
**Story:** W-01-S08 (settings — re-run setup wizard)

**Summary:** Choosing **Re-run setup wizard** in Settings does not restart the relay and open the setup wizard again as promised.

**Steps to reproduce:**
1. Complete initial setup (paste `DESK-` code, pick printer) so the tray app is running.
2. Open **Settings** from the tray menu.
3. Click **Re-run setup wizard** and confirm **Yes** on the prompt (“This clears saved settings and opens the setup wizard again…”).

**Expected:** App restarts (or returns to setup flow); setup wizard appears so the operator can paste a new desk code and printer.  
**Actual:** App does not restart; setup wizard does not appear. Operator remains on the tray with prior configuration still in effect (or app exits without wizard).

**Notes:** Settings copy promises wizard reopens after clearing saved settings (`SettingsForm.RerunSetup`). Printer save path uses the same `_requestRestart()` hook and may behave differently — compare when triaging. Logs: `%AppData%\EventPlatform\PrintRelay\logs\`. Workaround: quit tray, delete `%AppData%\EventPlatform\PrintRelay\settings.json`, relaunch exe.

---

## Resolved

<!-- Move fixed/wontfix/duplicate entries here, newest first. Keep original ID. -->

_(none yet)_
