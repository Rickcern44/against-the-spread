# CLAUDE.md

Entry point for Claude Code in this repo. `.agents.md` and `.github/copilot-instructions.md`
carry the same project context for their respective tools; keep the three in sync rather than
letting one drift.

## Project shape

A Blazor WebAssembly PWA + Azure Functions (isolated worker) API deployed together as a single
Azure Static Web App, backed by Azure Blob Storage.

```
src/
├── AgainstTheSpread.Core/        # models + storage/service contracts, zero project references
├── AgainstTheSpread.Functions/   # isolated-worker Functions API, references Core
├── AgainstTheSpread.Web/         # Blazor WASM PWA, references Core
└── AgainstTheSpread.Tests/       # xunit + bUnit, mirrors src/ 1:1, references all three
tests/                            # Playwright: specs, pages, helpers
infrastructure/terraform/         # Terraform (see infra docs for current apply status)
.devcontainer/                    # containerized dev environment
```

**Dependency rule: `Core` never references `Functions` or `Web`.** Confirm this by reading
`Core`'s own `.csproj` rather than assuming it — a `ProjectReference` from `Core` to either other
project is always wrong. `Functions` and `Web` each depend on `Core`; `Tests` depends on all
three.

The SDK version is pinned in `global.json` — treat that file as the single source of truth for
which .NET SDK this repo targets. Don't hard-code a runtime version in prose; it will drift the
moment the pin changes.

## Commands

| Task | Command |
|---|---|
| Build | `dotnet build AgainstTheSpread.sln` |
| Unit + component tests | `dotnet test AgainstTheSpread.sln` |
| Local dev stack (no E2E) | `./start-local.sh` … `./stop-local.sh` |
| Full local E2E | `./start-e2e.sh` then `cd tests && npm exec -- tsc -p tsconfig.json --noEmit && npm test` then `./stop-e2e.sh` |
| Environment sanity check | `./validate-environment.sh` |

Local stack ports: Azurite `10000`/`10001`/`10002`, Functions `7071`, Blazor dev server `5158`,
SWA CLI proxy `4280` — Playwright and the SWA-CLI-fronted app both target `4280`, not `5158`
directly. `start-e2e.sh` backs up and swaps `wwwroot/appsettings.Development.json` to point the
app at the proxy; only `stop-e2e.sh` restores it, so always pair the two.

## Tests

`src/AgainstTheSpread.Tests/` mirrors `src/` by folder (`Models/`, `Services/`, `Functions/`,
`Web/…`) — a test for `Services/StorageService.cs` lives in `Services/StorageServiceTests.cs`,
not in a new top-level file. Naming convention: `MethodName_Scenario_ExpectedBehavior`. Browser
flows live under `tests/specs/` (Playwright), with page objects in `tests/pages/` and shared
helpers in `tests/helpers/`.

## Agent roster

Job-scoped subagents in `.claude/agents/`, each with its own guardrails — read the file for the
one you're dispatching to before assuming what it covers:

| Agent | Reach for it when |
|---|---|
| `dotnet-upgrader` | Bumping a TargetFramework, a NuGet version, or `global.json` |
| `test-engineer` | Writing/fixing xunit or bUnit tests, or migrating an assertion/mocking library |
| `e2e-validator` | Playwright specs, Azurite fixture seeding, or browser-proving a deployed slot |
| `swa-infra` | Terraform, GitHub Actions workflows, SWA/devcontainer config, deployment tokens |
| `feature-builder` | A new end-to-end feature slice — model → endpoint → page, with tests |
| `docs-curator` | Reconciling or fixing this repo's documentation |

## How to prove a change works

Green `dotnet test` is necessary, not sufficient. In order:

1. `dotnet build` + `dotnet test AgainstTheSpread.sln` clean.
2. Full local E2E (`./start-e2e.sh` → `npm test` → `./stop-e2e.sh`) — this is the only local check
   that exercises Azurite, the isolated-worker Functions host, the Blazor app, and the SWA CLI
   proxy together, and it catches DI/wiring breakage that mocked unit tests cannot.
3. Open a PR. CI must build and test through the real pipeline before deploy — confirm the build
   job actually succeeds rather than assuming a green checkmark means what you expect.
4. Azure Static Web Apps creates a preview environment per PR. Exercise the real user-facing
   flows there in a browser before merging anything that touches deploy-affecting code. A preview
   slot inherits production app settings — including production storage — so only exercise
   read-only flows against it; never run an admin/upload/write path against a preview slot.
