# Privacy Policy — Event Platform Print Relay (Windows)

**Last updated:** 2026-07-04  
**Applies to:** `EventPlatform.PrintRelay` desktop application (Windows MSI)

This policy describes how the Print Relay **client application** handles information. It does not replace the privacy policy of your **Event Platform** deployment (the web service your organiser operates).

---

## Summary

Print Relay is a venue desk tool. It stores a desk credential and printer settings on the PC, polls your organiser's Event Platform instance for print jobs, and prints badge HTML locally. The app does **not** send data to Event Platform developers, SignPath, or other third-party analytics services. There is **no** remote error telemetry in the current release.

---

## Data stored on the device

| Data | Purpose | Location |
|---|---|---|
| Desk relay secret | Authenticate print-queue API requests | `%AppData%\EventPlatform\PrintRelay\` (encrypted at rest by Windows user profile) |
| API base URL | Reach your organiser's platform | Same settings store |
| Desk display name | Tray and settings UI | Same settings store |
| Selected printer name | Route print jobs | Same settings store |
| Local logs | Operator troubleshooting | `%AppData%\EventPlatform\PrintRelay\logs\` (`relay.log`, `startup.log`) |

Logs may include operational identifiers such as `job_id`, `desk_id`, `event_id`, and `registration_id` when a print job is processed. Logs **never** include the relay secret or full setup code. Log files are truncated in place when they exceed size limits (5 MB for `relay.log`, 256 KB for `startup.log`).

Settings persist across app updates and are **not** removed on MSI uninstall unless the operator deletes them manually.

---

## Data transmitted over the network

The app communicates **only** with the Event Platform instance configured during setup (`api_url` from the desk setup code):

- `GET /api/print-queue/pending` — fetch pending print jobs (includes `badge_html` for printing)
- `POST /api/print-queue/{id}/complete` — mark a job printed
- `POST /api/print-queue/{id}/failed` — report a print failure (plain-English message, max 500 characters)

All requests use `Authorization: Bearer <relay_secret>`. Badge content is rendered and printed locally; it is not sent to third parties.

**Who is the data controller for attendee/badge data?** Your event organiser, via their Event Platform deployment — not this open-source repository.

---

## Diagnostics export

**Copy diagnostics** (tray menu) copies a JSON snapshot to the clipboard **only when the operator chooses**. The export includes app version, desk name, printer name, connection state, recent job outcomes, and API hostname. It excludes the relay secret, setup code, and full API URL.

---

## Third parties

| Party | Role |
|---|---|
| **Your Event Platform host** | Print queue API; data processor/controller per organiser terms |
| **SignPath Foundation** | Code-signs release MSI builds in CI; does not receive end-user data from the running app |
| **Microsoft WebView2** | Local HTML rendering for badge print (standard Windows component) |

There is no advertising, sale of personal data, or cross-app tracking in the current release.

---

## Children's data

Print Relay is an event-operations tool for venue staff. It is not directed at children.

---

## Changes

Material changes to this policy will be committed to this repository with an updated date above.

---

## Contact

For questions about this application: open an issue on the [GitHub repository](https://github.com/deonvanaardt/event-platform-print-relay-windows).

For questions about attendee or event data: contact your **event organiser** (the operator of your Event Platform instance).
