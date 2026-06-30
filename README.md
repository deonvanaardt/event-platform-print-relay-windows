# Event Platform Print Relay — Windows

Signed Windows desktop app for silent badge printing at check-in desks. Polls the Event Platform print queue and prints server-rendered `badge_html` via **WebView2** — no client-side badge layout.

**Parent platform:** [event-management-platform](https://github.com/your-org/event-management-platform)  
**Spec:** `docs/PRINT_RELAY_WINDOWS_PRD.md` (v3.0) · integration checklist in platform `printrelay/INTEGRATION.md`

## Status

| Milestone | State |
|---|---|
| **Spike** — WebView2 silent CR80 fixture print | Scaffolded (`EventPlatform.PrintRelay.Spike`) |
| M1 — Setup wizard + staging poll + `badge_html` | Not started |
| M2 — Tray UI, settings, diagnostics | Not started |
| M3 — Signed MSI + CI release | Not started |

## Repository layout

```
src/
  EventPlatform.PrintRelay.Core/   # API client, setup code, settings (testable on macOS)
  EventPlatform.PrintRelay.Spike/  # Gate 3 WebView2 print spike (Windows only)
tests/
  EventPlatform.PrintRelay.Core.Tests/
docs/
  PRINT_RELAY_WINDOWS_PRD.md
  SPIKE.md
```

## Quick start (spike)

Requires **Windows** + .NET 8 SDK. See [docs/SPIKE.md](docs/SPIKE.md).

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- list-printers
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-test --printer "Microsoft Print to PDF"
```

## Development

```bash
# Cross-platform unit tests (setup code, API parsing, backoff)
dotnet test

# Windows publish (from macOS/Linux CI or dev machine)
dotnet publish src/EventPlatform.PrintRelay.Spike/EventPlatform.PrintRelay.Spike.csproj \
  -r win-x64 --self-contained true -p:PublishSingleFile=true -c Release
```

## Contracts (do not break without platform PRD §26 update)

- Print relay HTTP API — four endpoints under `/api/print-queue`
- Pending job shape — includes additive `badge_html` (required for production Windows relay)
- Desk setup code `DESK-` + Base64url JSON `v: 1`

JSON Schema artifacts will be consumed from the platform repo when E-05-S08 ships (`schemas/print-relay/`).

## Platform compatibility

Document minimum platform version per release. First production build requires platform staging with `badge_html` on `GET /api/print-queue/pending`.

## License

Proprietary — Event Platform.
