---
title: Sprint 4 — Kiosa brand icons + MSI installer branding
sprint: 4
stories: W-01-S12, W-01-S14
feature_requests: FR-001, FR-002
status: planned
created: 2026-07-18
updated: 2026-07-20
companion: SPRINT.md, BACKLOG.md, FEATURE_REQUESTS.md
---

# Sprint 4 — Kiosa brand icons + MSI installer branding

**Stories:** W-01-S12 (FR-001) · W-01-S14 (FR-002 + installer visuals)  
**Sprint board:** [`SPRINT.md`](../../SPRINT.md#sprint-4--kiosa-brand-icons--installer-version-fr-001-fr-002)  
**Acceptance:** [`BACKLOG.md`](../../BACKLOG.md) — W-01-S12, W-01-S14

## Source assets and rules

**Source of truth:** `kiosa-marketing/brand-pack/` (sibling repo on developer machine)

| File | Use in this project |
|---|---|
| `kiosa-logo-icon.svg` | **Primary** — tray, `.exe` icon, WinForms title bars, Task Manager, MSI `ARPPRODUCTICON` |
| `kiosa-logo-primary.svg` | Optional — setup wizard header; reference for installer dialog BMP layout |
| `kiosa-logo-monochrome.svg` | Fallback reference only if 16×16 tray accent is unreadable in testing |
| `Kiosa_Brand_Pack.md` | Rules: no shadows/gradients; icon-only below wordmark sizes; accent may blur below 24px |

Brand pack open item (section 8) notes ICO/PNG exports are **not yet generated** — we generate them in this repo from the SVG.

**Naming:** operator-facing product name is **Kiosa Print Relay** (UI, ARP, Task Manager metadata). Window/installer strings use Kiosa branding; exe filename stays `EventPlatform.PrintRelay.exe` for upgrade compatibility.

---

## Current gaps in this repo

`TrayApplicationContext.CreateIcon` clones `SystemIcons` (Warning / Error / Information) — not Kiosa branding.

- No `ApplicationIcon` in `EventPlatform.PrintRelay.App.csproj` — `.exe` and Start Menu shortcut inherit Windows default icon.
- Forms (`SetupWizardForm`, `StatusForm`, `SettingsForm`) do not set `Icon`.
- PRD §7.1 colour states (green / amber / red / grey) are not implemented; only 3 enum values exist in `RelayTrayIconState`.
- WiX `Package.wxs` Start Menu shortcut has no explicit `Icon` — it will pick up the embedded exe icon once `ApplicationIcon` is set.
- MSI installer uses **stock WiX** `WixUI_Minimal` banner and dialog BMPs — no Kiosa branding.
- No `Icon` / `ARPPRODUCTICON` in `Package.wxs` — Add/Remove Programs shows generic installer icon.
- Installer welcome/finish dialogs do not show product version text (FR-002).

---

## Target architecture

```mermaid
flowchart LR
  subgraph sources [kiosa-marketing brand-pack]
    svgIcon[kiosa-logo-icon.svg]
    svgPrimary[kiosa-logo-primary.svg optional]
  end

  subgraph repo [print-relay-windows]
  script[scripts/generate-app-icons.sh]
  ico[Assets/app.ico]
  pngs[Assets/tray/*.png]
  helper[RelayAppIcons.cs]
  csproj[ApplicationIcon in csproj]
  tray[TrayApplicationContext overlay]
  forms[WinForms Icon property]
  end

  subgraph windows [Windows runtime]
  exe[EventPlatform.PrintRelay.exe icon]
  trayArea[Notification area 16px]
  startMenu[Start Menu shortcut]
  end

  svgIcon --> script
  script --> ico
  script --> pngs
  ico --> csproj
  pngs --> helper
  helper --> tray
  helper --> forms
  csproj --> exe
  helper --> trayArea
  exe --> startMenu
```

### Installer branding (W-01-S14)

```mermaid
flowchart LR
  subgraph sources [brand assets]
    svgIcon[kiosa-logo-icon.svg]
    appIco[app.ico from Session 1]
  end

  subgraph generator [generate-app-icons.sh Session 3]
    banner[wix-banner.bmp 493x58]
    dialog[wix-dialog.bmp 493x312]
  end

  subgraph wix [Package.wxs]
    vars[WixUIBannerBmp WixUIDialogBmp]
    arp[Icon + ARPPRODUCTICON]
    ver[Version text on welcome/finish]
  end

  subgraph ui [Installer wizard]
    welcome[Welcome dialog]
    finish[Finish dialog]
    apps[Settings Apps list]
  end

  svgIcon --> banner
  svgIcon --> dialog
  appIco --> arp
  banner --> vars
  dialog --> vars
  vars --> welcome
  vars --> finish
  arp --> apps
  ver --> welcome
  ver --> finish
```

**Tray state approach:** keep the Kiosa icon; draw a small status dot (PRD colours: green `#16A34A`, amber `#D97706`, red `#DC2626`, grey `#78716F`) in the bottom-right corner at runtime via `System.Drawing`. Cache one `Icon` per state; dispose on swap (existing pattern in `AssignTrayIcon`).

---

## W-01-S14 — MSI installer branding (FR-002 + visuals)

**Current state:** `Package.wxs` sets `Version="$(var.ProductVersion)"` on the MSI package, but `WixUI_Minimal` shows stock WiX banner/dialog artwork and does not surface version on welcome or finish dialogs. App csproj is `0.4.2`; wixproj default `ProductVersion` is stale (`0.3.1`) — CI `release.yml` reads App version and passes `-p:ProductVersion` on build.

**Target:** Operator sees a Kiosa-branded install experience — custom banner strip, dialog side panel with Kiosa icon on brand background, Kiosa icon in Add/Remove Programs, and version text during interactive install.

### WiX BMP requirements (`WixUI_Minimal`)

| Asset | Size | WiX variable | Placement |
|---|---|---|---|
| Banner | **493 × 58** px, 24-bit BMP | `WixUIBannerBmp` | Top strip on inner dialogs (license, progress, etc.) |
| Dialog | **493 × 312** px, 24-bit BMP | `WixUIDialogBmp` | Left panel on welcome / finish / license |

Generate from brand pack: Kiosa icon centred on brand-appropriate solid background (no gradients per brand pack §3). Commit under `installer/EventPlatform.PrintRelay.Installer/Assets/brand/`.

### `Package.wxs` changes (Session 3)

```xml
<!-- Icon for Add/Remove Programs -->
<Icon Id="AppIcon" SourceFile="..\..\src\EventPlatform.PrintRelay.App\Assets\brand\app.ico" />
<Property Id="ARPPRODUCTICON" Value="AppIcon" />

<WixVariable Id="WixUIBannerBmp" Value="Assets\brand\wix-banner.bmp" />
<WixVariable Id="WixUIDialogBmp" Value="Assets\brand\wix-dialog.bmp" />
```

Version text: extend welcome subtitle and/or `WIXUI_EXITDIALOGOPTIONALTEXT` to include `$(var.ProductVersion)` (e.g. `Version 0.4.2`).

### Local MSI build (version sync)

```powershell
$v = (Select-Xml -Path src\EventPlatform.PrintRelay.App\EventPlatform.PrintRelay.App.csproj -XPath '//Version').Node.InnerText
dotnet build installer\EventPlatform.PrintRelay.Installer\EventPlatform.PrintRelay.Installer.wixproj -c Release -p:Platform=x64 -p:ProductVersion=$v
```

**Out of scope:** Changing `license.rtf` legal text; switching away from `WixUI_Minimal`; product rename in installer title strings.

---

## Asset layout (new files)

```
src/EventPlatform.PrintRelay.App/Assets/brand/
  kiosa-logo-icon.svg          # copied from kiosa-marketing (source reference)
  kiosa-logo-primary.svg       # optional wizard header
  app.ico                      # generated: 16, 32, 48, 256
  tray/
    base-32.png                # generated from SVG at 32px (overlay source)
installer/EventPlatform.PrintRelay.Installer/Assets/brand/
  wix-banner.bmp               # generated: 493×58 (Session 3)
  wix-dialog.bmp               # generated: 493×312 (Session 3)
scripts/
  generate-app-icons.sh        # SVG → PNG → ICO; Session 3 adds WiX BMP output
```

Commit the **generated** `.ico` and PNGs so Windows CI/build does not need SVG tooling. Document regeneration in script header.

---

## Implementation sessions

Work is split for the **Mac agent ↔ Windows operator** handoff model: each agent session ends with a push; Windows verification is **one step per reply**.

### Session 1 — Assets + base icon wiring (Mac agent)

**Goal:** Kiosa icon everywhere that does not need live state logic.

1. Copy SVGs from `kiosa-marketing/brand-pack/` into `Assets/brand/`.
2. Add `scripts/generate-app-icons.sh`; run on Mac to produce `app.ico` + `tray/base-32.png`.
3. Update `EventPlatform.PrintRelay.App.csproj`:
   - `<ApplicationIcon>Assets\brand\app.ico</ApplicationIcon>`
   - Embed tray PNGs as resources (`EmbeddedResource` or `Content` with copy).
4. Add `RelayAppIcons.cs` in App project:
   - `LoadAppIcon()` from embedded `app.ico`
   - `CreateTrayIcon(RelayTrayIconState)` — initially return base icon only (overlay in Session 2).
5. Replace `CreateIcon` / `SystemIcons` usage in `TrayApplicationContext.cs`.
6. Set `Icon = RelayAppIcons.LoadAppIcon()` on Setup, Status, Settings forms.
7. Log decision in `DECISIONS.md` (asset source, overlay strategy, committed generated ICO).
8. `CHANGELOG.md` Unreleased bullet on ship.

**Push** → operator **Session 1 Windows verify** (2–3 single-step replies):
- Pull → publish → confirm tray shows Kiosa (not yellow/blue system icons).
- Confirm `.exe` Properties → icon; open Status/Settings — title bar icon.
- `git log -1` SHA match.

### Session 2 — Tray state overlays + PRD alignment (Mac agent)

**Goal:** Colour-coded tray states per PRD §7.1.

1. Implement overlay drawing in `RelayAppIcons.CreateTrayIcon(state)` using PRD colours.
2. Map states in `RelayRuntime.GetTrayIconState()`:
   - Connected → green dot
   - BackingOff → amber dot
   - AuthError / printer missing → red dot
3. **Optional:** add `SetupRequired` enum + grey dot for incomplete settings (PRD row exists; enum missing today).
4. If 16×16 accent is muddy, switch tray base render to monochrome SVG variant at 16px only (brand pack §3 allowance).

**Push** → operator **Session 2 Windows verify** (2–3 replies):
- Force reconnect (disconnect network) → amber dot.
- Invalid printer / auth error → red dot.
- Normal poll → green dot.
- Confirm 16×16 readability in tray overflow (`^`).

### Session 3 — MSI installer branding + closure (Mac docs + Windows MSI)

**Goal:** Installed app and installer wizard both show Kiosa branding; version visible during setup.

**W-01-S12 (Mac):**
- Confirm no WiX change required for Start Menu icon if shortcut targets `.exe` (expected).
- Add installer acceptance line to `docs/INSTALLER.md`: Start Menu shortcut shows Kiosa icon.

**W-01-S14 (Mac):**
- Extend `scripts/generate-app-icons.sh` to emit `wix-banner.bmp` (493×58) and `wix-dialog.bmp` (493×312).
- Commit BMPs to `installer/EventPlatform.PrintRelay.Installer/Assets/brand/`.
- Update `Package.wxs`: `WixUIBannerBmp`, `WixUIDialogBmp`, `Icon` + `ARPPRODUCTICON`, version text on welcome/finish.
- Document local MSI build with `-p:ProductVersion=<App csproj Version>` (CI `release.yml` already reads App version).
- Add installer branding + version checks to `docs/INSTALLER.md` W-01-S09 checklist.

**Windows operator** (separate replies — MSI must be built on PC):
- Stop app → publish → `dotnet build` installer with `-p:ProductVersion` matching App → install MSI.
- Welcome screen: Kiosa dialog panel (not stock WiX art); version text visible.
- Progress/license screens: Kiosa banner strip on top.
- Finish screen: branded UI + version; **Start Print Relay now** still works.
- Settings → Apps: Print Relay shows Kiosa icon (ARP).
- Start Menu shortcut icon (W-01-S12).
- Upgrade path (install over existing) preserves branding and shows new version.

**Mac closure:**
- Mark W-01-S12 and W-01-S14 Done in `SPRINT.md`; update `CHANGELOG.md`.

**Out of scope for MVP (defer unless requested):**
- `--about` dialog custom logo.
- Setup wizard full `kiosa-logo-primary` lockup header.

---

## Session count summary

| Session | Where | Deliverable |
|---|---|---|
| **1** | Mac agent | SVG import, ICO generation, `ApplicationIcon`, base tray + form icons |
| **1a–1c** | Windows operator | Pull/publish/tray + exe icon verify (one step per reply) |
| **2** | Mac agent | Coloured status overlays + PRD state mapping |
| **2a–2c** | Windows operator | State colour verification at 16px |
| **3** | Mac + Windows | WiX banner/dialog BMPs, ARP icon, installer version, MSI verify, closure |

**Total:** 3 agent implementation sessions + 2 Windows verification rounds.

CI impact: **none** — Core tests unchanged; `release.yml` picks up new exe icon automatically on next tag.

---

## Risks and mitigations

| Risk | Mitigation |
|---|---|
| 16×16 amber accent unreadable | Test on hardware in Session 2; fall back to monochrome base at small sizes |
| Icon handle leaks | Keep existing dispose-on-swap in `AssignTrayIcon`; cache static base bitmap |
| kiosa-marketing drift | Copy SVGs with version comment in script; re-run generator when brand pack updates |
| WiX BMP wrong dimensions | Script validates 493×58 and 493×312 before write; test on Windows in Session 3 |
| Icon path in WiX harvest | Reference `app.ico` relative to installer project; validate in `dotnet build` on Windows |
| ARM64 publish path | No change — icon is architecture-neutral; same `win-x64` publish flow |

---

## Acceptance criteria

### W-01-S12 / FR-001

- [ ] `app.ico` contains 16, 32, 48, 256 sizes from `kiosa-logo-icon.svg`
- [ ] Tray `NotifyIcon` shows Kiosa icon with green/amber/red status dot
- [ ] `EventPlatform.PrintRelay.exe` and Start Menu shortcut show Kiosa icon
- [ ] Setup, Status, Settings windows show Kiosa icon in title bar
- [ ] Icon readable at 16×16 in tray overflow

### W-01-S14 / FR-002 (+ installer visuals)

- [ ] `WixUIBannerBmp` and `WixUIDialogBmp` show Kiosa artwork (not stock WiX) on welcome, license, progress, and finish
- [ ] `ARPPRODUCTICON` shows Kiosa icon in Settings → Apps
- [ ] Installer welcome and/or finish dialog shows product version matching App csproj / MSI `ProductVersion`
- [ ] Version and branding visible during normal interactive install
- [ ] `docs/INSTALLER.md` checklist includes installer branding and version steps

### Sprint closure

- [ ] `CHANGELOG.md`, `DECISIONS.md` (icons only), `SPRINT.md` updated on completion
