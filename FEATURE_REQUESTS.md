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

<!-- Add entries above this line, newest first. Next ID: FR-003 -->

---

## Accepted / in backlog

<!-- Move here when promoted to BACKLOG.md with W-01-Sxx. -->

### FR-002 — Version number on installer window

**Status:** accepted  
**Requested:** 2026-07-19  
**Requested by:** internal  
**Story:** W-01-S14 · **Sprint 4**

**Summary:** Show the app/product version (e.g. `0.4.1`) on the MSI installer UI so operators and support can confirm which build they are installing without opening the installed exe or GitHub.

**User story:** As a desk operator or IT person installing Print Relay, I want the installer window to show the version number so I can verify I have the correct release before or during setup.

**Acceptance hints:**
- WiX installer UI (e.g. welcome or finish dialog, or a consistent footer) displays the product version matching `EventPlatform.PrintRelay.App` / MSI `ProductVersion`.
- Version visible on a default install path without opening logs or file Properties.
- Installer wizard uses Kiosa-branded banner/dialog artwork and ARP icon (not stock WiX visuals) — same sprint, W-01-S14.
- Documented in `docs/INSTALLER.md` acceptance checklist.

**Notes:** WiX `Package.wxs` / `WixUI_Minimal` — `$(var.ProductVersion)` from App csproj via `release.yml` / `-p:ProductVersion`; `WixUIBannerBmp` / `WixUIDialogBmp` from brand pack. Bundled with Sprint 4 Session 3 (MSI work). Related: PRD installer finish UI (W-01-S09); Settings screen already shows app version post-install.

---

### FR-001 — Branded app icon (tray + Start Menu)

**Status:** accepted  
**Requested:** 2026-07-18  
**Requested by:** internal  
**Story:** W-01-S12 · **Sprint 4**

**Summary:** Replace placeholder/system icons with the Kiosa icon from `kiosa-marketing/brand-pack` in the notification area, on the executable, and on the installed Start Menu shortcut.

**User story:** As a desk operator, I want Print Relay to show a recognizable branded icon in the tray and Start Menu so I can find the app quickly among other programs at a busy venue.

**Acceptance hints:**
- Single branded icon asset (`.ico` with standard sizes: 16×16, 32×32, 48×48, 256×256) used consistently across the app.
- Tray `NotifyIcon` uses the Kiosa icon with coloured status-dot overlays for connected / reconnecting / error per PRD §7.1.
- Installed app executable and Start Menu shortcut show the same icon (MSI / `ApplicationIcon` in App project).
- Icon readable at 16×16 in the tray overflow area.

**Notes:** Design assets in `kiosa-marketing/brand-pack/`. Implementation plan: [`docs/plans/sprint-4-kiosa-brand-icons.md`](docs/plans/sprint-4-kiosa-brand-icons.md). Related: PRD §7.1 tray states, `docs/INSTALLER.md` Start Menu shortcut checklist.

---

## Deferred / declined

<!-- Move here with reason. Newest first. -->

_(none yet)_
