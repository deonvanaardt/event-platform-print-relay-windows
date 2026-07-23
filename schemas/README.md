# Platform contract schemas (pinned)

Draft-07 JSON Schema artifacts vendored from the Event Platform monorepo for cross-repo contract validation.

**Source of truth for runtime behaviour:** TypeScript in the platform repo (`/lib/print-queue/serialize.ts`, `/lib/print-desks/setup-code.ts`). These files are a **pinned copy** — see [`platform-pin.json`](platform-pin.json) for the exact platform commit.

## Files

| File | Describes |
|---|---|
| [`pending-job.response.json`](pending-job.response.json) | `GET /api/print-queue/pending` response `{ jobs: [...] }` |
| [`badge-render-input.json`](badge-render-input.json) | `badge_document` on each job (`$ref` from pending response) |
| [`desk-setup-code.v1.json`](desk-setup-code.v1.json) | Decoded setup code payload (`v: 1`) — not the `DESK-` wire string |
| [`pair-exchange.response.json`](pair-exchange.response.json) | `POST /api/v1/print-desks/pair` success response |

## Wire format

Admin **Copy setup code** produces `DESK-<base64url(JSON)>`. Decode per [`docs/PRINT_RELAY_WINDOWS_PRD.md`](../docs/PRINT_RELAY_WINDOWS_PRD.md) §5.2.

## Updating the pin

1. Identify the platform release or commit that changed contracts.
2. Copy `schemas/print-relay/*.json` and `fixtures/*` from that commit.
3. Update `platform-pin.json` `commit_sha` and `pinned_at`.
4. Run `dotnet test` — contract tests must pass.
5. Log a decision in `DECISIONS.md` if the bump requires a coordinated Windows release.

## Fixtures

| File | Purpose |
|---|---|
| [`fixtures/desk-setup-code.v1.valid.json`](fixtures/desk-setup-code.v1.valid.json) | Valid decoded setup payload |
| [`fixtures/pending-response.valid.json`](fixtures/pending-response.valid.json) | Single queued job with `badge_document` + `badge_html` |
| [`fixtures/pending-response.empty.json`](fixtures/pending-response.empty.json) | Empty queue |
| [`fixtures/pair-exchange.response.valid.json`](fixtures/pair-exchange.response.valid.json) | Pairing exchange success response |

## Related

- Platform consumer README: `event-management-platform/schemas/print-relay/README.md`
- Two-project checklist: [`INTEGRATION.md`](../INTEGRATION.md)
- Breaking changes: parent PRD §26
