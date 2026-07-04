# SignPath OSS approval — gap responses (W-01-S11)

**Purpose:** Copy-paste replies if SignPath still asks about gaps after proactive repo changes (see **Proactive measures** below).

**Related:** [`SIGNPATH.md`](SIGNPATH.md) · [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md) · [README Download](../README.md#download) · [GitHub Releases](https://github.com/deonvanaardt/event-platform-print-relay-windows/releases)

---

## Proactive measures (in repo)

These reduce or eliminate follow-up questions before approval:

| Gap | Built into repo |
|---|---|
| Releases page SignPath mention | `release.yml` writes a **Code signing** section (SignPath attribution + policy links) on every tag release; auto-generated changelog follows below |
| OSI license | Root [`LICENSE`](../LICENSE) (MIT); README **License** section updated |
| Code signing policy (SignPath ToS) | [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md) — required attribution, roles, privacy statement; linked from README **Download** |
| v0.3.1 release page | **Not in repo** — edit the existing GitHub Release description once (see Gap 1) |

---

## Gap 1 — Releases page does not mention SignPath (e.g. v0.3.1)

### What reviewers see

- **[README → Download](https://github.com/deonvanaardt/event-platform-print-relay-windows#download)** names the [SignPath Foundation](https://signpath.io/foundation) open-source signing program and links to [`docs/SIGNPATH.md`](SIGNPATH.md).
- **GitHub Releases** for tags so far (e.g. **v0.3.1**) use auto-generated release notes from commits/CHANGELOG. Those notes describe the unsigned MSI (Sprint 2 / W-01-S09) and do **not** repeat the SignPath paragraph from the README.
- **v0.3.1** was intentionally **unsigned** and marked **prerelease** while SignPath OSS approval was pending. Signing CI (W-01-S11) was wired afterward.

### Prepared reply (email / ticket)

> The project’s primary distribution and signing disclosure is in the repository README under **Download**, which states that release builds are code-signed via the SignPath Foundation open-source program and links to our SignPath runbook (`docs/SIGNPATH.md`). The **Download URL** in our application points to GitHub Releases, which is linked from that same README section.
>
> Release **v0.3.1** predates our first SignPath-signed build: it is an unsigned prerelease used for installer and staging validation (documented in our changelog and release workflow). Customer-ready signed MSIs will be published from **v0.4.0** onward once OSS approval and GitHub secrets are in place.
>
> Our Release workflow (`.github/workflows/release.yml`) adds an explicit **“Authenticode-signed via SignPath OSS”** line to the GitHub Release body for every **signed** tag release. We can also add a short SignPath note to older release pages (e.g. v0.3.1) after the first signed build if you prefer that visibility on the Releases tab itself.

### Follow-up actions (operator)

| When | Action |
|---|---|
| **After merging proactive changes** | Optionally edit **v0.3.1** GitHub Release description: add the same SignPath attribution line from [`CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md) and note it was an unsigned prerelease |
| **After first signed `v0.4.0` release** | Confirm release body includes **Code signing** section (CI sets this automatically) |

---

## Gap 2 — No `LICENSE` file at repository root

**Status:** Addressed — root [`LICENSE`](../LICENSE) (MIT) and README **License** section. Use replies below only if they ask about MSI EULA vs repo license.

### What reviewers used to see

- README **License** section: **“Proprietary — Event Platform.”**
- MSI installer includes an end-user license (`installer/EventPlatform.PrintRelay.Installer/license.rtf`) shown during setup.
- There is **no** `LICENSE` or `LICENSE.md` at the repo root (common expectation for SignPath OSS and GitHub’s license detector).

### Context for our reply

- This repo is a **public companion** to the Event Platform: source is open for audit, CI, and community signing workflows, but **distribution and commercial use** are governed by Event Platform, not a permissive OSS grant.
- SignPath OSS programs often expect an **OSI-approved** license file even when the README states proprietary terms — reviewers may follow up even if the application form did not ask.

### Prepared reply — if they accept proprietary / source-available (try first)

> Thank you for reviewing our application. The Windows Print Relay is a public source repository for a venue desk tool that ships as a signed MSI to Event Platform customers. We document licensing in the README (**Proprietary — Event Platform**) and in the MSI installer EULA (`license.rtf`).
>
> We applied to SignPath OSS because we publish open **source** on GitHub, use transparent CI signing, and do not use a self-managed code-signing certificate. We are happy to clarify our model: the repository is source-available for security review and OSS signing; the product is not offered under a permissive open-source license for unrestricted redistribution.
>
> If the SignPath OSS program requires a standard OSI license file at the repository root, please let us know which licenses you accept for “open source companion” or infrastructure tooling and we will align the repo (see follow-up options below).

### Prepared reply — if they require an OSI license on the repo

> We will add an OSI-approved license file at the repository root and update the README License section to match, scoped to **this Windows print relay repository** (not the separate Event Platform SaaS codebase). We will keep installer EULA language appropriate for end-user MSI distribution.

### Follow-up (only if MIT or MSI EULA is questioned)

| Topic | Response |
|---|---|
| **MIT vs SaaS** | This repo is the Windows relay only; Event Platform SaaS remains a separate product |
| **MSI `license.rtf`** | Venue deployment terms at install time; does not change MIT license on source |

---

## Quick reference — where SignPath is documented

| Location | SignPath mention |
|---|---|
| [README § Download](../README.md#download) | Yes — SignPath Foundation + `SIGNPATH.md` + `CODE_SIGNING_POLICY.md` |
| [`docs/CODE_SIGNING_POLICY.md`](CODE_SIGNING_POLICY.md) | Yes — required SignPath attribution + roles + privacy |
| [`LICENSE`](../LICENSE) | MIT (OSI-approved) |
| [`docs/SIGNPATH.md`](SIGNPATH.md) | Full operator runbook |
| [`docs/PRIVACY.md`](PRIVACY.md) | SignPath as CI signer; no end-user data |
| [`docs/INSTALLER.md`](INSTALLER.md) | Signed vs unsigned releases |
| `release.yml` (tag releases) | Release body **Code signing** section + generated changelog |
| GitHub Release v0.3.1 notes | No — edit manually once if desired |

---

## Checklist after approval

- [ ] Merge proactive OSS approval branch (`LICENSE`, `CODE_SIGNING_POLICY.md`, release body template)
- [ ] Optionally edit **v0.3.1** GitHub Release description (SignPath attribution — not automated)
- [ ] Complete [`SIGNPATH.md`](SIGNPATH.md) Steps 2–5 (dashboard, secrets, `v0.4.0`)
- [ ] Verify signed release body on GitHub includes **Code signing** section
