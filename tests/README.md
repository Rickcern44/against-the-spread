# Against The Spread Playwright tests

These tests exercise the browser against the local Blazor, Functions, SWA CLI, and Azurite stack.

## Covered flows

- Weekly week selection, six picks, two-stage generation/download, and XLSX validation.
- Bowl spread/confidence/outright picks, duplicate-confidence rejection, two-stage generation/download, and XLSX validation.
- GIS UI callback and same-origin `X-Google-ID-Token` use without ordinary `Authorization` or browser persistence.
- Weekly upload validation errors remain visible with their offending spreadsheet cell; the admin stays signed in and no success message is shown.
- Installed-iOS PWA file-sharing behavior and fallback download.

Weekly and bowl browser suites seed deterministic JSON directly into local Azurite through `helpers/seed-azurite-fixtures.ts`. This is test harness setup, not an application auth bypass. Protected production APIs remain fail-closed and are covered by .NET authorization tests. The GIS browser test mocks Google's browser callback and the identity endpoint; it is not proof of real Google production authentication.

## Prerequisites

- Node.js 22+
- .NET SDK (version pinned in `../global.json`)
- Azure Functions Core Tools 4
- Azurite
- Azure Static Web Apps CLI

Install browser dependencies:

```bash
cd tests
npm ci
npx playwright install chromium
```

## Run

From the repository root:

```bash
./start-e2e.sh
cd tests
npm exec -- tsc -p tsconfig.json --noEmit
npm test
cd ..
./stop-e2e.sh
```

The stack uses ports 10000 (Azurite), 7071 (Functions), 5158 (Blazor), and 4280 (SWA proxy). Run it only where those listeners are owned by this checkout. `stop-e2e.sh` uses process-name matching and is intended for an isolated local or CI runner.

## Configuration

Playwright uses `http://localhost:4280`, Chromium, one worker, failure screenshots/video, and first-retry traces. The fixture seeder accepts only the standard `UseDevelopmentStorage=true` Azurite connection string and fails closed for remote storage targets.

Real release acceptance additionally requires production Google token validation, allowlist authorization, upload/readback safety, service-worker update checks, and a physical installed-iPhone share-sheet/Save-to-Files check. See `../CONSOLIDATION_RUNBOOK.md`.
