# MSI backup

Built installers copied here are committed to GitHub for backup.

**Current file:** `EventPlatform.PrintRelay.msi` (replace when you build a newer version).

**On Windows after building** — tell the agent to commit, or run:

```powershell
Copy-Item -Force (Get-ChildItem installer\EventPlatform.PrintRelay.Installer\bin -Recurse -Filter *.msi | Select-Object -First 1).FullName releases\msi\EventPlatform.PrintRelay.msi
git add releases/msi/EventPlatform.PrintRelay.msi
git commit -m "Update MSI backup"
git push
```

Do not put files in `artifacts/` — that folder is not committed.
