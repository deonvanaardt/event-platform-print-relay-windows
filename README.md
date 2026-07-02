# Event Platform Print Relay — Windows

Signed Windows desktop app for silent badge printing at check-in desks. Polls the Event Platform print queue and prints server-rendered `badge_html` via **WebView2** — no client-side badge layout.

**Parent platform:** [event-management-platform](https://github.com/deonvanaardt/event-management-platform)  
**Spec:** [`docs/PRINT_RELAY_WINDOWS_PRD.md`](docs/PRINT_RELAY_WINDOWS_PRD.md) v3.0  
**Integration:** [`INTEGRATION.md`](INTEGRATION.md) (two-project checklist)

## Planning (read before coding)

| Document | Purpose |
|---|---|
| [`SPRINT.md`](SPRINT.md) | Current sprint scope |
| [`BACKLOG.md`](BACKLOG.md) | W-01 stories and acceptance criteria |
| [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) | M0–M4 phases |
| [`CHANGELOG.md`](CHANGELOG.md) | What already ships |
| [`DECISIONS.md`](DECISIONS.md) | Implementation-time choices |
| [`Tech_Stack_Decision_Record.md`](Tech_Stack_Decision_Record.md) | Tools, bans, hard rules |

Cursor agents: rules in [`.cursor/rules/`](.cursor/rules/) mirror the platform repo discipline.

## Status

| Milestone | State |
|---|---|
| **Spike** — WebView2 silent fixture print (Gate 3) | **Passed** — A5 physical sign-off |
| **Sprint 1** — Schema CI + M1 staging integration | **In progress** — see `SPRINT.md` |
| M2 — Tray UI, settings, diagnostics | **Shipped** (W-01-S07, W-01-S08) |
| M3 — Signed MSI + CI release | Not started |

## Repository layout

```
src/
  EventPlatform.PrintRelay.Core/   # API client, setup code, settings (testable on macOS)
  EventPlatform.PrintRelay.Spike/  # Gate 3 WebView2 print spike (Windows only)
tests/
  EventPlatform.PrintRelay.Core.Tests/
schemas/                           # Pinned platform JSON Schema (see platform-pin.json)
docs/
  PRINT_RELAY_WINDOWS_PRD.md
  SPIKE.md
  STAGING_INTEGRATION.md
```

## Quick start

**Unit tests (macOS/Linux):**

```bash
dotnet test
```

**Spike (Windows only):** see [`docs/SPIKE.md`](docs/SPIKE.md).

```powershell
dotnet run --project src/EventPlatform.PrintRelay.Spike -- list-printers
dotnet run --project src/EventPlatform.PrintRelay.Spike -- print-test --printer "Microsoft Print to PDF"
```

**Git hooks (optional):**

```bash
git config core.hooksPath .githooks
```

## Contracts (do not break without parent PRD §26)

- Print relay HTTP API — four endpoints under `/api/print-queue`
- Pending job shape — requires `badge_html` for production Windows relay
- Desk setup code `DESK-` + Base64url JSON `v: 1`

Pinned schemas: [`schemas/`](schemas/) · platform commit in [`schemas/platform-pin.json`](schemas/platform-pin.json)

## Platform compatibility

Document minimum platform version per release. First production build requires platform staging with `badge_html` on `GET /api/print-queue/pending`.

## License

Proprietary — Event Platform.
