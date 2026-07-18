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

<!-- Add entries above this line, newest first. Next ID: BUG-001 -->

_(none yet)_

---

## Resolved

<!-- Move fixed/wontfix/duplicate entries here, newest first. Keep original ID. -->

_(none yet)_
