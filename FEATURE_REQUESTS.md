# Feature requests

Ideas for **new or changed behaviour** that are not yet scoped as backlog stories.  
Use this for venue feedback, operator UX improvements, and “nice to have” items before they become [`BACKLOG.md`](BACKLOG.md) work.

**Newest entries at the top** of each section.

---

## What goes here vs elsewhere

| Item | Document |
|---|---|
| New capability or UX change (not yet a sprint story) | **This file** |
| Something broken vs spec or acceptance criteria | [`BUGS.md`](BUGS.md) |
| Committed sprint story with acceptance criteria | [`BACKLOG.md`](BACKLOG.md) |
| Product requirement already in spec | [`docs/PRINT_RELAY_WINDOWS_PRD.md`](docs/PRINT_RELAY_WINDOWS_PRD.md) |
| Shipped capability | [`CHANGELOG.md`](CHANGELOG.md) |

When a request is approved for implementation, create or reference a `W-01-Sxx` story in `BACKLOG.md`, add the story ID here, and move the entry to **Accepted / in backlog**.

---

## Status values

| Status | Meaning |
|---|---|
| **proposed** | Logged; not reviewed |
| **under review** | Evaluating fit, scope, and priority |
| **accepted** | Approved — story added to backlog (link `W-01-Sxx`) |
| **deferred** | Valid but not now — note trigger to revisit |
| **declined** | Out of scope for Windows relay or PRD — explain why |

---

## Entry template

```markdown
### FR-NNN — Short title

**Status:** proposed | under review | accepted | deferred | declined  
**Requested:** YYYY-MM-DD  
**Requested by:** e.g. venue operator, internal, agent  
**Story:** (when accepted) W-01-Sxx  

**Summary:** One sentence — what problem this solves for desk staff.  

**User story:** As a … I want … so that …  

**Acceptance hints:** Bullet list of how we would know it is done (not full story AC).  

**Notes:** PRD section, platform dependency, security/privacy impact.  
**Declined reason:** … (if declined)
```

**Rules:** No relay secrets or full setup codes in this file.

---

## Proposed / under review

<!-- Add entries above this line, newest first. Next ID: FR-002 -->

### FR-001 — Branded app icon (tray + Start Menu)

**Status:** proposed  
**Requested:** 2026-07-18  
**Requested by:** internal  

**Summary:** Replace placeholder/system icons with a proper Event Platform Print Relay icon in the notification area and on the installed Start Menu shortcut.

**User story:** As a desk operator, I want Print Relay to show a recognizable branded icon in the tray and Start Menu so I can find the app quickly among other programs at a busy venue.

**Acceptance hints:**
- Single branded icon asset (`.ico` with standard sizes: 16×16, 32×32, 48×48, 256×256) used consistently across the app.
- Tray `NotifyIcon` uses the branded icon (with state variants or overlays for connected / reconnecting / error per PRD §7.1, if designed).
- Installed app executable and Start Menu shortcut show the same icon (MSI / `ApplicationIcon` in App project).
- Icon readable at 16×16 in the tray overflow area.

**Notes:** Today the tray uses cloned `SystemIcons` (Warning / Error / Information); no `ApplicationIcon` is set on the App project. Design asset needed from product/brand. Related: PRD §7.1 tray states, `docs/INSTALLER.md` Start Menu shortcut checklist, W-01-S10 physical sign-off.

---

## Accepted / in backlog

<!-- Move here when promoted to BACKLOG.md with W-01-Sxx. -->

_(none yet)_

---

## Deferred / declined

<!-- Move here with reason. Newest first. -->

_(none yet)_
