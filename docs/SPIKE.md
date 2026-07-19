# WebView2 silent-print spike (Gate 3)

Pre-coding gate **3** from `docs/PRINT_RELAY_WINDOWS_PRD.md` §12: prove silent printing of fixture HTML to a **named** printer with **no system dialog**.

The spike currently uses an **A5 portrait** test fixture (`148mm × 210mm`) for office-paper validation via `print-test`. The production tray app resolves page size dynamically from each job's `@page` CSS (fallback: `badge_document` format metadata, then CR80 default). Local fixtures cover all platform template formats:

| Fixture | Format | `@page` size |
|---|---|---|
| `test-badge-cr80.html` | CR80 | 85.6 × 54 mm |
| `test-badge-a6-landscape.html` | A6 Landscape | 148 × 105 mm |
| `test-badge-a5-portrait.html` | A5 Portrait | 148 × 210 mm |
| `test-badge-a5-landscape.html` | A5 Landscape | 210 × 148 mm |

App fixtures live under `src/EventPlatform.PrintRelay.App/Fixtures/`; Spike copies under `src/EventPlatform.PrintRelay.Spike/Fixtures/` (Gate 3 `print-test` still uses `test-badge-a5.html`, same as A5 portrait).

Tray UI, MSI packaging, and code signing are **out of scope** until this gate passes.

**Status:** Gate 3 **passed** (2026-07-01). Next work: Sprint 1 in [`SPRINT.md`](../SPRINT.md) — W-01-S03 schema CI, then M1 staging integration ([`docs/STAGING_INTEGRATION.md`](STAGING_INTEGRATION.md)).

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

Print arbitrary HTML (e.g. `badge_html` saved from staging). Page size is resolved from `@page` CSS in the file (same `BadgePageDimensionResolver` as production):

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-html `
  --printer "Microsoft Print to PDF" `
  --file .\src\EventPlatform.PrintRelay.App\Fixtures\test-badge-a6-landscape.html
```

For CR80:

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-html `
  --printer "Microsoft Print to PDF" `
  --file .\src\EventPlatform.PrintRelay.App\Fixtures\test-badge-cr80.html
```

For A5 landscape:

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-html `
  --printer "Microsoft Print to PDF" `
  --file .\src\EventPlatform.PrintRelay.App\Fixtures\test-badge-a5-landscape.html
```

## Pass criteria

| Check | How to verify |
|---|---|
| No print dialog | Run `print-test`; badge HTML is rendered to A5 PDF, then sent to the printer. **Microsoft Print to PDF** writes `spike-print-*.pdf` in the current directory. |
| Named printer | `--printer` must match an entry from `list-printers`; default printer is not used silently |
| Page dimensions | `print-test` uses A5 (`148mm × 210mm`). `print-html` auto-resolves from `@page` in the file. Production app uses the same resolver (`@page` → `badge_document` → CR80). |
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
