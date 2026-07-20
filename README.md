# Event Platform Print Relay — Windows

Signed Windows desktop app for silent badge printing at check-in desks. Polls the Event Platform print queue and prints server-rendered `badge_html` via **WebView2** — no client-side badge layout.

**Parent platform:** [event-management-platform](https://github.com/deonvanaardt/event-management-platform)  
**Spec:** [`docs/PRINT_RELAY_WINDOWS_PRD.md`](docs/PRINT_RELAY_WINDOWS_PRD.md) v3.0  
**Integration:** [`INTEGRATION.md`](INTEGRATION.md) (two-project checklist)

## Download

Windows installers (`.msi`) are published on **[GitHub Releases](https://github.com/deonvanaardt/event-platform-print-relay-windows/releases)**.

Release builds will be Authenticode-signed for customer distribution once a signing provider is in place. SignPath OSS was **declined 2026-07-18** (visibility); **paid signing is deferred** until the first paying customer. Unsigned prereleases are available now for staging. Plan and checklist: [`docs/SIGNPATH.md`](docs/SIGNPATH.md). **Code signing policy:** [`docs/CODE_SIGNING_POLICY.md`](docs/CODE_SIGNING_POLICY.md).

**Privacy:** [`docs/PRIVACY.md`](docs/PRIVACY.md)

## Planning (read before coding)

| Document | Purpose |
|---|---|
| [`SPRINT.md`](SPRINT.md) | Current sprint scope |
| [`BACKLOG.md`](BACKLOG.md) | W-01 stories and acceptance criteria |
| [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) | M0–M4 phases |
| [`CHANGELOG.md`](CHANGELOG.md) | What already ships |
| [`BUGS.md`](BUGS.md) | Known defects (open and resolved) |
| [`FEATURE_REQUESTS.md`](FEATURE_REQUESTS.md) | Ideas not yet backlog stories |
| [`DECISIONS.md`](DECISIONS.md) | Implementation-time choices |
| [`Tech_Stack_Decision_Record.md`](Tech_Stack_Decision_Record.md) | Tools, bans, hard rules |

Cursor agents: rules in [`.cursor/rules/`](.cursor/rules/) mirror the platform repo discipline. **Mac agent ↔ Windows testing:** see [`.cursor/rules/git-sync.mdc`](.cursor/rules/git-sync.mdc).

## Status

| Milestone | State |
|---|---|
| **Spike** — WebView2 silent fixture print (Gate 3) | **Passed** — A5 physical sign-off |
| **Sprint 1** — Schema CI + M1 staging integration | **Closed** — staging E2E smoke passed 2026-07-03 |
| M2 — Tray UI, settings, diagnostics | **Shipped** (W-01-S07, W-01-S08) |
| **Sprint 2** — Unsigned MSI + release CI | **Closed** (W-01-S09) |
| **Sprint 3** — SignPath OSS signing CI | **Blocked** (W-01-S11 — CI wired; SignPath declined 2026-07-18; reapply or paid signing) |
| **Sprint 4** — Kiosa brand icons + MSI installer branding | **Active** (W-01-S12, W-01-S14 — parallel to Sprint 3) |

## Repository layout

```
src/
  EventPlatform.PrintRelay.Core/   # API client, setup code, settings (testable on macOS)
  EventPlatform.PrintRelay.Spike/  # Gate 3 WebView2 print spike (Windows only)
tests/
  EventPlatform.PrintRelay.Core.Tests/
schemas/                           # Pinned platform JSON Schema (see platform-pin.json)
installer/                         # WiX MSI project (W-01-S09)
releases/msi/                      # Committed MSI backups (optional)
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

**MSI installer (Windows):** see [`docs/INSTALLER.md`](docs/INSTALLER.md).

**App (Windows — after agent pushes from Mac):**

```powershell
git fetch origin
git checkout feature/sprint-1-m1
git pull origin feature/sprint-1-m1
git log -1 --oneline          # must match commit agent reported
taskkill /IM EventPlatform.PrintRelay.exe /F
dotnet publish src\EventPlatform.PrintRelay.App -c Release -r win-x64 --self-contained -o artifacts\app
Get-Content .\artifacts\app\build-info.txt
.\artifacts\app\EventPlatform.PrintRelay.exe
```

Full checklist: [`.cursor/rules/git-sync.mdc`](.cursor/rules/git-sync.mdc).

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

[MIT](LICENSE) — Event Platform Print Relay (this repository).

The MSI installer shows a separate end-user license during setup (`installer/EventPlatform.PrintRelay.Installer/license.rtf`) for venue deployment terms.
