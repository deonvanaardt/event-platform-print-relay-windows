# MSI release backups (local)

Committed copies of built installers for handoff and GitHub backup. **Not** used by CI — GitHub Actions still builds from source.

## What goes here

| File | Example |
|---|---|
| Single embedded MSI only | `EventPlatform.PrintRelay-0.3.1-win-x64.msi` |

Do **not** commit `cab1.cab` — builds after `f60b5e8` embed the cabinet in the MSI.

## After building on Windows

From repo root, after `docs/INSTALLER.md` publish + WiX build:

```powershell
cd C:\Users\Deon\event-platform-print-relay-windows

$version = ([xml](Get-Content src\EventPlatform.PrintRelay.App\EventPlatform.PrintRelay.App.csproj)).Project.PropertyGroup.Version | Select-Object -First 1
$msi = Get-ChildItem installer\EventPlatform.PrintRelay.Installer\bin -Recurse -Filter *.msi | Select-Object -First 1
$dest = "releases\msi\EventPlatform.PrintRelay-$version-win-x64.msi"

Copy-Item -Force $msi.FullName $dest
Write-Host "Copied to $dest"
Get-Item $dest | Select-Object FullName, Length, LastWriteTime
```

Commit and push when you want the MSI on GitHub:

```powershell
git add releases/msi/EventPlatform.PrintRelay-$version-win-x64.msi
git commit -m "Add MSI backup $version"
git push origin feature/sprint-2-m3
```

## Size note

MSI files are large (~100 MB+ self-contained). That is expected for this backup folder.
