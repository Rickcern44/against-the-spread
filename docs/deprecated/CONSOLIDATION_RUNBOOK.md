# Consolidation deployment and rollback runbook

This runbook governs the v1.1 consolidation into the original production site. It overrides older generic Terraform/deployment examples where they disagree with the verified live topology.

## Protected production identity

- GitHub: `quaz579/against-the-spread`, branch `main`
- Static Web App: `against-the-spread-rg/swa-against-the-spread`
- Production origin: `https://agreeable-river-0e2f38010.3.azurestaticapps.net`
- Data storage: `against-the-spread-rg/stprdagnstthesprd`, private container `gamefiles`
- Deployment secret name: `AZURE_STATIC_WEB_APPS_API_TOKEN_AGREEABLE_RIVER_0E2F38010`

Do not apply the checked-in Terraform to this existing production topology: it is historical and does not describe the live managed-API deployment. Never substitute the v1.1 resource group, storage account, workflow, state backend, or deployment token.

## Pre-deployment gate

1. Confirm the candidate branch is based on the current remote `main` and both repositories are clean.
2. Run `dotnet test AgainstTheSpread.sln -c Release`, `dotnet publish` for Web and Functions, TypeScript checking, and all Playwright tests.
3. Require an independent review of the complete candidate diff.
4. Export both `gamefiles` containers byte-for-byte to protected storage outside either resource group. Record blob names, lengths, ETags and SHA-256 values in a private manifest and prove a restore to an isolated private container.
5. Enumerate every Azure File share in fork storage. Classify each share, export every file (including the known `ats-v11-prod-func-80b1` share) or prove it empty, include it in the encrypted backup manifest, and exercise restore before permitting storage deletion.
6. Export the fork Terraform state and sanitized configuration inventory outside `ats-v11-prod-rg`. State and app-setting values are secrets; keep them mode-restricted, out of Git, logs, and reports.
7. Record the current original SWA environment timestamp and a rollback artifact containing the Web, Functions and effective `staticwebapp.config.json` from one exact commit.
8. Confirm the shared Google Web client authorizes the original production origin. Keep the fork origin during the rollback window.

## Deployment

Open and merge a pull request into original `main`. The original workflow validates .NET, TypeScript, and live local browser flows before using the original deployment token. Verify:

```bash
HOME=/home/rdpuser gh pr checks <PR_NUMBER> -R quaz579/against-the-spread --watch
HOME=/home/rdpuser gh run view <RUN_ID> -R quaz579/against-the-spread
HOME=/home/rdpuser gh api repos/quaz579/against-the-spread/commits/main --jq .sha
```

The remote `main` SHA, successful deployment run `headSha`, and deployed production artifact must match. A workflow success or HTTP 200 alone is not release acceptance.

## Production acceptance

Use only reversible test activity. Picks downloads are in-memory and do not persist user picks. Do not overwrite production lines merely to test them.

- Load available weeks and a weekly dataset; select six games; generate and validate the XLSX.
- Load bowl lines; complete spread/confidence/outright picks; generate, then explicitly download and validate the XLSX.
- Verify missing, malformed, forged, wrong-audience and non-admin credentials are rejected on `current-admin` and both upload routes without storage mutation.
- With the allowlisted account, complete real Google GIS sign-in and authorize both upload controls. If an upload must be exercised, first back up the exact key and use an isolated/reversible dataset approved for production.
- Sign out and verify the token is cleared from component memory; confirm no token appears in browser storage or logs.
- Verify a fresh service worker activates and stale cached UI cannot restore obsolete auth.
- From an installed iPhone PWA, generate weekly and bowl files, use **Download File**, confirm the native share sheet and Save to Files, cancel/retry, then edit a bowl pick and confirm the stale workbook is invalidated.
- Re-read all preexisting original blobs and compare to the pre-deployment manifest.

If real Google or iPhone interaction is unavailable, stop at `blocked_user_only`; keep the fork repository and infrastructure intact.

## Rollback

Rollback uses the captured Free-compatible Web/API/config artifact, not the December 2025 custom-auth configuration (Azure now rejects that configuration on Free). Redeploy the captured artifact with the original deployment token, read back the environment, and repeat public and auth rejection checks. Restore data only for a proven storage mutation, key-by-key from the private manifest; never blanket-restore over newer production uploads.

## Conditional fork retirement

Retirement starts only after the production acceptance gate and backup/restore proof pass. Freshly inventory all resource IDs before deletion.

1. Preserve the fork `gamefiles` objects, `against-the-spread-v1.1.tfstate`, and every Azure File share outside the fork resource group; verify hashes and a full restore, or retain explicit proof that a share was empty.
2. Disable the fork deployment workflow before any deprecation push so Azure cannot be recreated or refreshed.
3. Add and push a deprecation notice linking the original site, remove only the fork deployment secret, then archive (do not delete) `quaz579/against-the-spread-v1.1`; verify the workflow is inert and `archived: true`.
4. Delete only verified fork-exclusive resources in `ats-v11-prod-rg`: `ats-v11-prod-web`, `atsv11prodst`, and `ats-v11-prod-ai`. Also verify removal of managed resource group `ai_ats-v11-prod-ai_09d6a3bc-75bd-4db1-b1c4-7bd41cd318ed_managed` and workspace `managed-ats-v11-prod-ai-ws`; do not match resources by a broad name pattern.
5. Preserve all original resources, shared Google OAuth configuration, `ats-tfstate-rg/atstfstate`, `DefaultResourceGroup-CUS`, and every `rg-*-cus-atsv2` resource.
6. Read back fork absence, archived/inert repository state, original resource/settings presence, original blob hashes, and original production flows.
