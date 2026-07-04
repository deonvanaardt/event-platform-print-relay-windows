# Code signing policy

**Applies to:** `EventPlatform.PrintRelay` Windows MSI releases from this repository.

Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

Setup and CI integration: [`SIGNPATH.md`](SIGNPATH.md)

---

## What we sign

- **Artifact:** WiX MSI installer for Windows x64 (`EventPlatform.PrintRelay.*.msi`)
- **Contents:** Self-contained `EventPlatform.PrintRelay.exe`, native dependencies (including Pdfium and WebView2 loader), and installer metadata
- **Source:** Built only from tagged commits on `main` via GitHub Actions (`.github/workflows/release.yml`, tags `v*`)

Unsigned MSI builds may be published as **prereleases** when SignPath credentials are not configured; they are for internal and staging use only.

---

## Roles

| Role | Responsibility | Who |
|---|---|---|
| **Authors** | Maintain source code; trigger trusted CI builds from the repository | Repository maintainers with write access |
| **Reviewers** | Review release workflow and signing configuration changes | Repository maintainers |
| **Approvers** | Approve signing requests in the SignPath project dashboard | Designated project owner (SignPath approver role) |

Release signing is automated: GitHub Actions submits the MSI to SignPath after a successful trusted build. Manual signing with a local `.pfx` is not used.

---

## Verification

Download installers only from [GitHub Releases](https://github.com/deonvanaardt/event-platform-print-relay-windows/releases) for this repository. Signed releases include Authenticode signatures applied by SignPath. Verification steps: [`INSTALLER.md`](INSTALLER.md) (signed MSI section).

---

## Privacy

This program does not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

Print Relay contacts only the Event Platform instance configured during desk setup (print-queue API). It does not send end-user data to SignPath. Full detail: [`PRIVACY.md`](PRIVACY.md).
