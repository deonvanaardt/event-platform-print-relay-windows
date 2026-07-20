# MSI installer runbook (W-01-S09)

Build, install, and verify the **unsigned** WiX MSI. Customer distribution requires **W-01-S11** (SignPath OSS signing).

**PRD:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §4.1, §6  
**Signing follow-up:** [`docs/SIGNPATH.md`](SIGNPATH.md) (W-01-S11)

---

## Build locally (Windows)

Prerequisites: .NET 8 SDK, WiX Toolset 5 (installed automatically via `WixToolset.Sdk` on first `dotnet build`).

```powershell
cd C:\Users\Deon\event-platform-print-relay-windows

# 1. Publish app as a folder (not single-file) — required for MSI harvest
dotnet publish src\EventPlatform.PrintRelay.App `
  -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=false `
  -o artifacts\publish

# 2. Confirm Pdfium is present
@(
  "artifacts\publish\runtimes\win-x64\native\pdfium.dll",
  "artifacts\publish\x64\pdfium.dll",
  "artifacts\publish\pdfium.dll"
) | ForEach-Object { if (Test-Path $_) { "OK $_" } }

# 3. Build MSI (x64 package — required for ProgramFiles64Folder)
$v = (Select-Xml -Path src\EventPlatform.PrintRelay.App\EventPlatform.PrintRelay.App.csproj -XPath '//Version').Node.InnerText
dotnet build installer\EventPlatform.PrintRelay.Installer\EventPlatform.PrintRelay.Installer.wixproj -c Release -p:Platform=x64 -p:ProductVersion=$v

# MSI output (under installer project bin):
Get-ChildItem installer\EventPlatform.PrintRelay.Installer\bin -Recurse -Filter *.msi

# 4. Backup copy for GitHub (fixed path — overwrite each build)
Copy-Item -Force (Get-ChildItem installer\EventPlatform.PrintRelay.Installer\bin -Recurse -Filter *.msi | Select-Object -First 1).FullName releases\msi\EventPlatform.PrintRelay.msi
```

**Tray smoke vs MSI:** `artifacts\app` (zip handoff to print-test PC) and `artifacts\publish` (MSI harvest) are **different folders**. Always publish to `artifacts\publish` before `dotnet build` on the installer project.

**Same-version upgrade during dev:** rebuilding MSI at the same `ProductVersion` may leave stale DLLs in Program Files (symptom: `MissingMethodException` at runtime). **Uninstall** from Settings → Apps, then install again; or bump `<Version>` in the App csproj before rebuilding MSI.

---

## CI release

Workflow: [`.github/workflows/release.yml`](../.github/workflows/release.yml)

| Trigger | SignPath secrets set | Result |
|---|---|---|
| `workflow_dispatch` | No | Builds MSI; uploads workflow artifact only |
| `workflow_dispatch` | Yes | Builds MSI; signs via SignPath; uploads artifact |
| Push tag `v*` | No | **Prerelease** GitHub Release with unsigned `.msi` |
| Push tag `v*` | Yes | **Stable** GitHub Release with Authenticode-signed `.msi` |

Signing uses [SignPath OSS](SIGNPATH.md) — **no** `.pfx` or `signtool` in this repo.

---

## Signed MSI verification (W-01-S11)

After downloading a release built with SignPath secrets configured:

```powershell
Get-AuthenticodeSignature .\EventPlatform.PrintRelay.msi
# Expect: Status Valid, SignerCertificate present

# After install
Get-AuthenticodeSignature "C:\Program Files\EventPlatform\PrintRelay\EventPlatform.PrintRelay.exe"
# Expect: Status Valid
```

- SmartScreen should **not** show “Windows protected your PC” / unknown publisher (or significantly less friction than unsigned builds).
- Re-run the full [W-01-S09 acceptance checklist](#w-01-s09-acceptance-checklist-windows-hardware) on the signed MSI.

---

## Install

1. Double-click `EventPlatform.PrintRelay.msi` (or downloaded release asset).
2. Accept the UAC prompt (per-machine install to Program Files).
3. SmartScreen may warn **"Windows protected your PC"** for unsigned builds — choose **More info** → **Run anyway** (internal/staging only).
4. Follow the installer wizard: **Welcome** → accept license → **Install** → progress → **Finish**.
5. On the finish screen, confirm the success message appears. **Start Kiosa Print Relay now** is checked by default; click **Finish** to launch the tray app immediately (or uncheck to close without launching).

**Install location:** `C:\Program Files\EventPlatform\PrintRelay\`

The release MSI is a **single file** (`EmbedCab="yes"`). You do not need to copy `cab1.cab` separately. If an older build prompts for a CAB, rebuild after pulling latest or copy `cab1.cab` next to the MSI as a temporary workaround.

**Start Menu:** Kiosa → Kiosa Print Relay

**Auto-start:** installer writes HKCU  
`Software\Microsoft\Windows\CurrentVersion\Run\EventPlatform.PrintRelay`  
= quoted path to `EventPlatform.PrintRelay.exe` for the **user who ran the installer**.

**Settings:** `%AppData%\EventPlatform\PrintRelay\` is **not** removed on uninstall (desk secret and printer choice persist).

**WebView2 cache:** `%LocalAppData%\EventPlatform\PrintRelay\WebView2\` (writable; required when installed under Program Files).

---

## W-01-S09 acceptance checklist (Windows hardware)

Run after local or CI-built MSI:

- [ ] MSI installs to `Program Files\EventPlatform\PrintRelay\` without errors
- [ ] Installer wizard shows welcome, license, progress, and finish screens
- [ ] Finish screen shows success text and **Start Kiosa Print Relay now** checkbox (checked by default)
- [ ] Click **Finish** with checkbox checked — tray app starts (setup wizard on first run); no second UAC on the launched app
- [ ] Uncheck **Start Kiosa Print Relay now**, click **Finish** — installer closes without launching the app
- [ ] Start Menu shortcut launches the tray app with Kiosa icon (icon near clock)
- [ ] Settings → Apps lists **Kiosa Print Relay** (publisher **Kiosa**) with Kiosa icon
- [ ] Task Manager shows **Kiosa Print Relay** as the process display name (exe file remains `EventPlatform.PrintRelay.exe`)
- [ ] `reg query HKCU\Software\Microsoft\Windows\CurrentVersion\Run /v EventPlatform.PrintRelay` shows the install path
- [ ] Sign out and back in (or reboot) — app auto-starts to tray without a main window
- [ ] Staging smoke: setup code → poll → print `badge_html` → job `printed` on platform (see [`STAGING_INTEGRATION.md`](STAGING_INTEGRATION.md))
- [ ] Uninstall from Settings → Apps — Program Files removed; Run key removed; AppData settings still present
- [ ] Install newer MSI over older — upgrade succeeds; settings preserved
- [ ] Silent install still works: `msiexec /i EventPlatform.PrintRelay.msi /qn` (no UI; no launch-on-finish)

Mark **W-01-S09** Done in `SPRINT.md` when all boxes pass.

---

## W-01-S12 acceptance checklist (Kiosa icons + product rename)

Run after publish (tray smoke) or MSI rebuild (installed-app checks):

**Published app (build VM smoke or print-test PC zip)**

- [ ] Tray notification area shows Kiosa icon (not yellow/blue system icons)
- [ ] Connected → green status dot; reconnecting → amber; error → red
- [ ] Icon readable at 16×16 in tray overflow (`^` near clock)
- [ ] `EventPlatform.PrintRelay.exe` Properties → icon is Kiosa
- [ ] Task Manager → process display name **Kiosa Print Relay** (Details may still show `EventPlatform.PrintRelay.exe` path)
- [ ] Setup / Status / Settings windows: Kiosa title-bar icon and **Kiosa Print Relay** titles

**MSI install (print-test PC or build VM)**

- [ ] Settings → Apps: **Kiosa Print Relay**, publisher **Kiosa**, Kiosa icon
- [ ] Start Menu: **Kiosa → Kiosa Print Relay** with Kiosa icon
- [ ] Upgrade install over previous MSI preserves settings; ARP shows new name/icon

Mark **W-01-S12** Done in `SPRINT.md` when all boxes pass.

---

## W-01-S11 acceptance checklist (signed release)

Run after SignPath OSS approval, GitHub secrets configured, and tag `v0.4.0` pushed:

**CI**

- [ ] GitHub Actions **Release** workflow: **Sign MSI via SignPath** step succeeds
- [ ] GitHub Release for `v0.4.0` is **not** marked prerelease
- [ ] Release asset is `.msi` (signed)

**Windows (download release MSI)**

- [ ] `Get-AuthenticodeSignature .\EventPlatform.PrintRelay.msi` → `Status: Valid`
- [ ] Install MSI — SmartScreen does **not** require “Run anyway” for unknown publisher
- [ ] `Get-AuthenticodeSignature "C:\Program Files\EventPlatform\PrintRelay\EventPlatform.PrintRelay.exe"` → `Status: Valid`
- [ ] Full [W-01-S09 acceptance checklist](#w-01-s09-acceptance-checklist-windows-hardware) passes on signed MSI
- [ ] Staging smoke: setup code → poll → print → job `printed`

Mark **W-01-S11** Done in `SPRINT.md` when all boxes pass.

---

## SignPath OSS

Release CI signs via SignPath when GitHub secrets are configured. Setup and first signed release: [`docs/SIGNPATH.md`](SIGNPATH.md).
