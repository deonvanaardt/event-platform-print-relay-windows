# SignPath OSS signing (W-01-S11)

**Status:** CI wired in `.github/workflows/release.yml` — **pending** SignPath OSS approval, dashboard setup, GitHub secrets, and first signed `v0.4.0` release verification on Windows.

Customer-ready releases require Authenticode signatures. This repo uses **[SignPath OSS](https://signpath.io/solutions/open-source-community)** — SignPath holds the certificate. **Do not** add a self-managed `.pfx` to GitHub Actions secrets.

**PRD:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §4.2  
**Installer runbook:** [`INSTALLER.md`](INSTALLER.md)

---

## Step 1 — Apply for SignPath OSS (operator)

1. Go to https://signpath.io/solutions/open-source-community
2. Submit application with:
   - **Repository URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows` (adjust org if forked)
   - **Homepage URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows`
   - **Download URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows/releases` (README mentions SignPath Foundation)
   - **Privacy Policy URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows/blob/main/docs/PRIVACY.md`
   - **Description (copy/paste):**  
     _Windows tray application for silent badge printing at event check-in desks. Polls the Event Platform print queue and prints server-rendered HTML via WebView2. Open-source companion to the Event Platform; distributed as a signed WiX MSI on GitHub Releases._
3. Wait for approval (typically a few business days)

---

## Step 2 — SignPath dashboard (after approval)

Create and configure the project:

| Setting | Value |
|---|---|
| Project name | Event Platform Print Relay |
| Repository URL | Same GitHub repo URL as above |
| **Trusted build** | GitHub Actions |
| Workflow file | `.github/workflows/release.yml` |
| Tag pattern | `v*` |
| Signing policy slug | `release-signing` (or SignPath default — record the slug you choose) |
| Artifact configuration slug | `msi-installer` |
| Artifact type | **MSI deep-sign** — inner PE files (`EventPlatform.PrintRelay.exe`, native DLLs) **and** outer `.msi` |

**Artifact config tip:** Build a sample MSI locally ([`INSTALLER.md`](INSTALLER.md)), upload it in SignPath, and use **Update from artifact sample** to generate deep-sign rules for the harvested `Program Files\EventPlatform\PrintRelay\` layout.

### Dashboard checklist

- [ ] SignPath OSS application approved
- [ ] Project linked to `event-platform-print-relay-windows`
- [ ] Signing policy created (slug → `SIGNPATH_SIGNING_POLICY_SLUG`)
- [ ] MSI artifact configuration `msi-installer` with deep-sign for EXE + MSI
- [ ] Trusted build: GitHub Actions · `.github/workflows/release.yml` · tags `v*`

---

## Step 3 — GitHub repository secrets

Add these in **Settings → Secrets and variables → Actions** (API tokens only — never a `.pfx`):

| Secret | Source |
|---|---|
| `SIGNPATH_API_TOKEN` | SignPath → user API token with submitter permission on the project |
| `SIGNPATH_ORG_ID` | SignPath organization UUID |
| `SIGNPATH_PROJECT_SLUG` | Project slug from dashboard |
| `SIGNPATH_SIGNING_POLICY_SLUG` | Signing policy slug (e.g. `release-signing`) |

**CLI (optional):** from a machine with `gh` authenticated:

```powershell
gh secret set SIGNPATH_API_TOKEN --repo deonvanaardt/event-platform-print-relay-windows
gh secret set SIGNPATH_ORG_ID --repo deonvanaardt/event-platform-print-relay-windows
gh secret set SIGNPATH_PROJECT_SLUG --repo deonvanaardt/event-platform-print-relay-windows
gh secret set SIGNPATH_SIGNING_POLICY_SLUG --repo deonvanaardt/event-platform-print-relay-windows
```

Each command prompts for the value interactively.

---

## Step 4 — CI behaviour (`release.yml`)

After unsigned MSI build:

1. Upload MSI as workflow artifact (`upload_msi`)
2. If `SIGNPATH_API_TOKEN` is set → `signpath/github-action-submit-signing-request@v1` with `artifact-configuration-slug: msi-installer`
3. GitHub Release (tag `v*` only) attaches **signed** MSI when signing succeeded; otherwise **unsigned** fallback
4. **Signed** tag releases: `prerelease: false` (customer-ready)
5. **Unsigned** fallback: `prerelease: true` with warning in release notes

Reference: [stamp-verify release.yml](https://github.com/stamp-verify/stamp-verify/blob/main/.github/workflows/release.yml)

---

## Step 5 — First signed release (operator)

1. Pull latest with W-01-S11 CI changes and app version **0.4.0**
2. Confirm all four SignPath secrets are set
3. Tag and push:

```powershell
git tag v0.4.0
git push origin v0.4.0
```

4. In GitHub Actions → **Release** workflow: confirm **Sign MSI via SignPath** succeeds
5. On Windows, download the release asset and run verification in [`INSTALLER.md`](INSTALLER.md) § Signed MSI

---

## Step 6 — Platform (E-05-S09)

- [ ] Set `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` to the **signed** GitHub Release asset URL (stable tag, not prerelease)

---

## Explicitly out of scope

- `signtool` with repo-stored `.pfx` / `WINDOWS_CODE_SIGNING_CERTIFICATE*` secrets
- Manual signing as a pre-event step
