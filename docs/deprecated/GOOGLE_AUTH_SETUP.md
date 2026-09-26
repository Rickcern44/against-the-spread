# Google Identity Services Admin Authentication

The admin page uses Google Identity Services (GIS) on the Azure Static Web Apps Free plan. The browser receives a short-lived signed Google ID token and sends it only to the protected same-origin APIs. The Functions validate the token and enforce the admin allowlist.

## Google Cloud configuration

Use the existing Web application OAuth client.

Under **Authorized JavaScript origins**, keep the rollback origin during the transition and include the original production origin:

```text
https://agreeable-river-0e2f38010.3.azurestaticapps.net
```

Do not add a redirect URI for this JavaScript-callback flow. Keep the existing legacy callback configuration through the rollback window.

The OAuth client ID is public and is present in the frontend configuration. The Google client secret is not used and must not be added to the repository, browser configuration, or Azure settings.

If the consent screen is in Testing mode, keep the intended admin Google account in the test-user list.

## Azure configuration

The original managed API must retain these application settings on `swa-against-the-spread`:

- `GOOGLE_CLIENT_ID`: exact expected Google token audience.
- `ADMIN_EMAILS`: comma-separated allowlist of verified Google email addresses.
- `AZURE_STORAGE_CONNECTION_STRING`: private original game-file storage.
- `APPLICATIONINSIGHTS_CONNECTION_STRING`: original telemetry.

The checked-in Terraform is historical and does not describe the verified live original topology; do not apply it to configure production. Manage this migration through the existing SWA settings and original deployment workflow without copying fork resource identities. Do not configure SWA custom authentication. The Free plan does not support the old custom `auth` block. An existing legacy `GOOGLE_CLIENT_SECRET` setting is unused by GIS and may remain during the rollback window.

## Authorization behavior

- Missing, malformed, expired, wrongly signed, or wrong-audience credentials return `401`.
- Tokens without a verified, non-empty email return `401`.
- A valid Google identity not present in `ADMIN_EMAILS` returns `403`.
- `/api/current-admin`, `/api/upload-lines`, and `/api/upload-bowl-lines` require
  the Google ID token in `X-Google-ID-Token`. The managed SWA proxy replaces
  `Authorization`, so the application must not use that header for this token.
- The identity route avoids Azure Functions' reserved `/api/admin*` prefix.
- Public picks and lines APIs remain anonymous.
- `X-MS-CLIENT-PRINCIPAL` is not trusted.

## Verification

1. Open `/admin` on the deployed original origin.
2. Confirm the rendered Google button reaches Google's account chooser.
3. Sign in with an allowlisted account.
4. Confirm the admin page displays the server-verified email.
5. Exercise both upload forms.
6. Sign out and confirm the credential is discarded and the upload UI disappears.

Never put Google credentials in logs, URLs, screenshots, `localStorage`, or `sessionStorage`.
