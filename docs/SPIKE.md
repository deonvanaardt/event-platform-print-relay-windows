# WebView2 silent-print spike (Gate 3)

Pre-coding gate **3** from `docs/PRINT_RELAY_WINDOWS_PRD.md` §12: prove silent printing of CR80 fixture HTML to a **named** printer with **no system dialog**.

Tray UI, MSI packaging, and code signing are **out of scope** until this gate passes.

## Prerequisites

- Windows 10 (1803+) or Windows 11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [WebView2 Evergreen Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (preinstalled on current Windows builds; install manually if missing)

## Build

```powershell
dotnet build EventPlatform.PrintRelay.sln -c Release
```

Cross-compile from macOS/Linux (Spike EXE requires Windows to run):

```bash
dotnet publish src/EventPlatform.PrintRelay.Spike/EventPlatform.PrintRelay.Spike.csproj \
  -r win-x64 --self-contained true -p:PublishSingleFile=true -c Release
```

## Spike commands

List installed printers:

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- list-printers
```

Print bundled CR80 test fixture (no dialog):

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-test `
  --printer "Microsoft Print to PDF" `
  --desk-name "Main entrance"
```

Print arbitrary HTML (e.g. `badge_html` saved from staging):

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-html `
  --printer "Microsoft Print to PDF" `
  --file .\badge.html
```

## Pass criteria

| Check | How to verify |
|---|---|
| No print dialog | Run `print-test`; only WebView2 `PrintAsync` is used — no `ShellExecute` print verb |
| Named printer | `--printer` must match an entry from `list-printers`; default printer is not used silently |
| CR80 dimensions | Fixture uses `@page { size: 85.6mm 54mm; }` aligned with platform `cr80` preset |
| Background/colour | `ShouldPrintBackgrounds = true`; CSS includes `print-color-adjust: exact` |
| Physical sign-off | Repeat on a USB/network badge printer before customer deployment |

## Next milestone (M1)

After platform staging ships `badge_html` (E-05-S06):

1. Decode a `DESK-` setup code from admin **Copy setup code**
2. Poll `GET /api/print-queue/pending` with the embedded secret
3. Print `badge_html` via the same `WebView2SilentPrinter` path
4. `POST /api/print-queue/{id}/complete`

## macOS development

Core library unit tests run without Windows:

```bash
dotnet test tests/EventPlatform.PrintRelay.Core.Tests
```

WebView2 printing requires a Windows VM (Parallels/UTM) or hardware.
