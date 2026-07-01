# WebView2 silent-print spike (Gate 3)

Pre-coding gate **3** from `docs/PRINT_RELAY_WINDOWS_PRD.md` §12: prove silent printing of fixture HTML to a **named** printer with **no system dialog**.

The spike currently uses an **A5** test fixture (`148mm × 210mm`) for office-paper validation. Production badges remain **CR80** (`85.6mm × 54mm`); see `Fixtures/test-badge-cr80.html` when CR80 stock is available.

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

Print bundled A5 test fixture (no dialog):

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-test `
  --printer "Microsoft Print to PDF" `
  --desk-name "Main entrance"
```

Set the printer driver paper size to **A5** in *Printer properties → Preferences* when testing on physical paper.

Print arbitrary HTML (e.g. `badge_html` saved from staging):

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-html `
  --printer "Microsoft Print to PDF" `
  --file .\badge.html
```

## Pass criteria

| Check | How to verify |
|---|---|
| No print dialog | Run `print-test`; badge HTML is rendered to A5 PDF, then sent to the printer. **Microsoft Print to PDF** writes `spike-print-*.pdf` in the current directory. |
| Named printer | `--printer` must match an entry from `list-printers`; default printer is not used silently |
| Page dimensions | Fixture uses `@page { size: 148mm 210mm; }` (A5). Production uses CR80 — see `test-badge-cr80.html`. |
| Background/colour | `ShouldPrintBackgrounds = true`; CSS includes `print-color-adjust: exact` |
| Physical sign-off | Repeat on a USB/network printer with **A5** loaded (or CR80 badge stock when available). ZPL-only thermal drivers may print blank pages with HTML — use a driver that supports Windows GDI/HTML printing. |

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
