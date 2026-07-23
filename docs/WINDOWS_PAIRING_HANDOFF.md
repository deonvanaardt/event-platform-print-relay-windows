---
title: Windows Print Relay — Pairing code handoff (S18-S04)
date: 2026-07-23
status: action-required
platform_story: S18-S04 (FR-009)
windows_story: W-01-S15
---

# Windows Print Relay — Pairing code handoff

**Read this in the Windows repo (`event-platform-print-relay-windows`) before changing the setup wizard.**

The Kiosa platform (this monorepo) now uses **8-character pairing codes** as the only admin setup path. The Windows app must exchange that code for desk credentials before polling.

Full contract: [`INTEGRATION.md`](../INTEGRATION.md) §4 (Pairing code exchange).

---

## What changed on the platform (July 2026)

| Before | After |
|--------|--------|
| Admin copies long `DESK-…` setup code (embeds full `relay_` secret) | Admin copies **8-char pairing code** (e.g. `K7MNP2QR`) |
| Windows decodes `DESK-` locally | Windows calls **`POST /api/v1/print-desks/pair`** |
| Code shown once; regenerate invalidates immediately | Pairing code **view again** anytime; does **not** rotate secret |
| No revoke in UI | **Revoke desk** stops relay; **Regenerate secret** invalidates old credentials |

Admin UI no longer shows `DESK-` codes or raw secrets. **`DESK-` v1 decode can remain in the Windows app** for any laptops already configured — optional, no production users yet.

---

## Windows repo — required changes

### 1. Backlog story: W-01-S15

**Title:** Pairing code setup (exchange API)

**Acceptance criteria:**

- Setup wizard accepts an **8-character** code (Crockford alphabet: `23456789ABCDEFGHJKMNPQRSTVWXYZ`, case-insensitive).
- On Continue, if input matches pairing format → `POST {api_url}/api/v1/print-desks/pair` with `{ "code": "<normalized>" }`.
- On 200, persist `secret`, `api_url`, `desk_name`, `desk_id` locally (same shape as today after `DESK-` decode).
- Validate with `GET {api_url}/api/print-queue/pending` + `Authorization: Bearer {secret}` before printer step.
- Plain-English errors for 400 / 429 / network (see INTEGRATION.md §4).
- Tray shows `desk_name` from exchange response.
- **Optional:** Keep `DESK-` prefix branch for backward compat (low priority — no production users).

### 2. Wizard UX

| Field label | `Pairing code` (not “setup code”) |
| Helper text | “Enter the 8-character code from Kiosa Print desks.” |
| Input | Single line, max 8 chars, uppercase normalization on submit |
| API base URL | **Do not ask the operator.** Use the URL you POST to for exchange (see below). |

**How to choose `api_url` for exchange:**

The pairing endpoint is on the **same origin** as the platform app:

| Environment | Exchange URL |
|-------------|----------------|
| Production | `https://app.kiosa.io/api/v1/print-desks/pair` |
| Staging / preview | `https://<preview-host>/api/v1/print-desks/pair` |
| Local dev | `http://localhost:3000/api/v1/print-desks/pair` |

**MVP approach:** Add a hidden/advanced **Platform URL** field defaulting to `https://app.kiosa.io`, or an environment baked into the MSI (`appsettings.Production.json`). Organisers on preview must point relay at the preview host — document in release notes.

The exchange response includes `api_url` — **use that value** for all subsequent print-queue calls.

### 3. Input routing (pseudocode)

```csharp
var input = Normalize(userInput); // trim, uppercase

if (input.StartsWith("DESK-", StringComparison.Ordinal))
{
    // Legacy v1 — optional
    config = DecodeDeskSetupCodeV1(input);
}
else if (IsPairingCodeFormat(input)) // exactly 8 chars, allowed alphabet
{
    config = await ExchangePairingCodeAsync(platformBaseUrl, input);
}
else
{
    ShowError("Enter a valid 8-character pairing code from Kiosa.");
}
```

`IsPairingCodeFormat`: `^[23456789ABCDEFGHJKMNPQRSTVWXYZ]{8}$`

### 4. Exchange HTTP client

```http
POST /api/v1/print-desks/pair
Content-Type: application/json

{ "code": "K7MNP2QR" }
```

**Success (200):**

```json
{
  "secret": "relay_…",
  "api_url": "https://app.kiosa.io",
  "desk_name": "Main entrance",
  "desk_id": "uuid"
}
```

**Errors (platform envelope PRD §16.3):**

```json
{ "error": "INVALID_INPUT", "message": "…", "required_scope": null }
```

| HTTP | `error` | Operator message |
|------|---------|------------------|
| 400 | `INVALID_INPUT` | “This pairing code is invalid, expired, or already used. Ask your organiser for a new code.” |
| 429 | `RATE_LIMIT_EXCEEDED` | “Too many attempts — wait a minute and try again.” (+ honour `Retry-After`) |
| 5xx / network | — | “Could not connect — check internet and try again.” |

After exchange, verify: `GET {api_url}/api/print-queue/pending` with `Authorization: Bearer {secret}`.

### 5. Local storage

No schema change — store the same fields you already persist after `DESK-` decode:

- `secret` (relay bearer token)
- `api_url`
- `desk_name`
- `desk_id` (add if not already stored — useful for diagnostics)

### 6. Version matrix

Update Windows `RELEASE.md` / installer about dialog:

| Print Relay version | Platform | Setup method |
|-------------------|----------|--------------|
| **&lt; 1.1.0** (current) | Any | `DESK-` setup code only |
| **≥ 1.1.0** (target) | ≥ Sprint 18 deploy | 8-char pairing code (primary) |

Set minimum platform version in Windows repo once pairing ships.

---

## Test plan (both repos)

### A. Platform-only (curl — no Windows build yet)

```bash
# 1. Create desk in admin → copy pairing code from dialog (e.g. ABCD1234)
# 2. Exchange (replace host + code):
curl -sS -X POST "https://app.kiosa.io/api/v1/print-desks/pair" \
  -H "Content-Type: application/json" \
  -d '{"code":"ABCD1234"}' | jq .

# Expect: secret, api_url, desk_name, desk_id

# 3. Poll pending with returned secret:
curl -sS "https://app.kiosa.io/api/print-queue/pending" \
  -H "Authorization: Bearer relay_…"
```

### B. End-to-end (after Windows W-01-S15)

1. Platform: create desk → pairing code shown.
2. Windows: enter pairing code → Connected in tray.
3. Platform: **Send sample print** → job **Printed** in Print activity.
4. Platform: **Show pairing code** again → new code works; relay **stays connected** (old code unused until exchanged elsewhere).
5. Platform: **Regenerate secret** → relay disconnects → new pairing code reconnects.
6. Platform: **Revoke desk** → relay shows auth error.

---

## References (Kiosa monorepo)

| Artifact | Path |
|----------|------|
| Exchange route | `app/api/v1/print-desks/pair/route.ts` |
| Exchange lib | `lib/print-desks/exchange-pairing-code.ts` |
| Pairing format | `types/print-desk-pairing.ts`, `DECISIONS.md` (2026-07-22) |
| Admin UI | `components/admin/print-desks-panel.tsx` |
| Integration contract | `printrelay/INTEGRATION.md` §4 |
