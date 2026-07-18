# SignPath OSS signing (W-01-S11)

**Status:** CI wired in `.github/workflows/release.yml` — SignPath Foundation OSS application **declined 2026-07-18** (insufficient external reputation signals; not a quality judgment). Reapply when the project has broader public visibility, or use a paid signing provider for customer releases sooner.

Customer-ready releases require Authenticode signatures. **Preferred path:** **[SignPath OSS](https://signpath.io/solutions/open-source-community)** — SignPath holds the certificate. **Do not** add a self-managed `.pfx` to GitHub Actions secrets until a paid-signing decision is recorded in `Tech_Stack_Decision_Record.md`.

## Application status (2026-07-18)

SignPath Foundation (Phillip Deng) declined the OSS application. Reviewers cited insufficient external verification signals:

- Community adoption (GitHub stars, forks, contributors)
- Independent references (Reddit, Stack Overflow, YouTube, etc.)
- External articles, blog posts, or institutional backing
- Sustained user engagement

**Repo at application time:** public, MIT license, code signing policy, privacy policy, unsigned `v0.3.1` release — policy gaps were not cited.

**Reapply when:** 2+ visibility signals are stronger (e.g. stars/forks, release downloads, third-party mention). SignPath invited a future reapplication.

**Until signed releases exist:** GitHub Releases remain **unsigned prereleases**; operators use SmartScreen → *More info → Run anyway* (see [`INSTALLER.md`](INSTALLER.md)).

### Alternatives if customer MSI is needed before reapply

| Option | Rough cost | Notes |
|---|---|---|
| **Reapply to SignPath** | Free | Wait for reputation signals; CI already wired |
| **Azure Artifact Signing** | ~$10/month | EU/UK **organizations** eligible; GitHub Actions integration |
| **Certum cloud OV** | ~€100–200/year | `signtool` / cloud token; requires Tech Stack + CI update |

**Operator decision (2026-07-18):** UK sole trader — defer paid signing until first paying customer. Until then: unsigned prereleases only. When triggered: **Certum Open Source Code Signing in the Cloud** (~$50–58/year); update `Tech_Stack_Decision_Record.md` and `release.yml` before purchase.

### When first paying customer arrives (Certum OSS checklist)

1. Purchase [Certum Open Source Code Signing in the Cloud](https://certum.store/code-signing.html) (~$58/year) — MIT repo should qualify; have GitHub URL and `LICENSE` ready for verification.
2. Complete Certum identity verification (see [required documents](https://support.certum.eu/en/code-signing-required-documents/)).
3. Install SimplySign Desktop on your Windows PC; sign publish output (`.exe`, native DLLs) then outer `.msi` before tagging, **or** wire automated signing in `release.yml` (community `ssign` / SimplySign-on-runner — evaluate at that time).
4. Tag `v0.4.0` (or next version), verify with `Get-AuthenticodeSignature` per [`INSTALLER.md`](INSTALLER.md).
5. Set platform `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` to the signed release asset (E-05-S09).

**PRD:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §4.2  
**Installer runbook:** [`INSTALLER.md`](INSTALLER.md)

---

## Step 1 — Apply for SignPath OSS (operator)

**Approval follow-ups:** If SignPath asks about Releases page wording (e.g. v0.3.1) or a missing `LICENSE` file, use prepared replies in [`SIGNPATH_OSS_APPROVAL.md`](SIGNPATH_OSS_APPROVAL.md).

1. Go to https://signpath.io/solutions/open-source-community
2. Submit application with:
   - **Repository URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows` (adjust org if forked)
   - **Homepage URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows`
   - **Download URL:** `https://github.com/deonvanaardt/event-platform-print-relay-windows/releases` (README **Download** + [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md); release CI adds SignPath text to every tag release body)
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
