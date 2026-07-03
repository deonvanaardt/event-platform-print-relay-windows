# SignPath OSS signing (W-01-S11)

**Status:** Not started — blocked on SignPath OSS approval and W-01-S09 MSI pipeline green.

Customer-ready releases require Authenticode signatures. This repo uses **[SignPath OSS](https://signpath.io/solutions/open-source-community)** — SignPath holds the certificate. **Do not** add a self-managed `.pfx` to GitHub Actions secrets.

**PRD:** `docs/PRINT_RELAY_WINDOWS_PRD.md` §4.2  
**Installer runbook:** [`INSTALLER.md`](INSTALLER.md)

---

## Operator: apply for SignPath OSS (do now)

1. Go to https://signpath.io/solutions/open-source-community
2. Submit application with this repository URL and project description
3. Wait for approval (typically a few business days)

---

## W-01-S11 acceptance (when approved)

### SignPath dashboard

- [ ] Create project linked to `event-platform-print-relay-windows`
- [ ] Signing policy for release builds (e.g. `release-signing`)
- [ ] Artifact configuration for `.msi` (e.g. `msi-installer`)
- [ ] Trusted build: GitHub Actions, workflow `.github/workflows/release.yml`, tags `v*`

### GitHub repository secrets (API tokens only)

| Secret | Description |
|---|---|
| `SIGNPATH_API_TOKEN` | SignPath API token |
| `SIGNPATH_ORG_ID` | Organization UUID |
| `SIGNPATH_PROJECT_SLUG` | Project slug |
| `SIGNPATH_SIGNING_POLICY_SLUG` | Policy slug |

### CI changes (`release.yml`)

After unsigned MSI build:

1. Upload MSI as workflow artifact
2. Run `signpath/github-action-submit-signing-request@v1` with `github-artifact-id`
3. Attach **signed** MSI to GitHub Release (may remove prerelease flag for stable tags)
4. Verify on Windows: `Get-AuthenticodeSignature` shows valid signature; SmartScreen shows trusted publisher

Reference: [stamp-verify release.yml](https://github.com/stamp-verify/stamp-verify/blob/main/.github/workflows/release.yml)

### Platform (E-05-S09)

- [ ] Set `NEXT_PUBLIC_PRINT_RELAY_WINDOWS_MSI_URL` to the **signed** GitHub Release asset URL

---

## Explicitly out of scope

- `signtool` with repo-stored `.pfx` / `WINDOWS_CODE_SIGNING_CERTIFICATE*` secrets
- Manual signing as a pre-event step
