# GitHub Copilot Coding Agent Environment

This document explains the GitHub Copilot coding agent environment configuration for the Against The Spread application.

## What is the Copilot Coding Agent Environment?

When GitHub Copilot coding agent works on tasks in this repository, it operates in its own ephemeral development environment powered by GitHub Actions. This environment is automatically configured using the `.github/workflows/copilot-setup-steps.yml` workflow file.

## Configuration File

The `copilot-setup-steps.yml` workflow is located at:
```
.github/workflows/copilot-setup-steps.yml
```

This file must be present on the default branch for GitHub Copilot to use it.

## What Gets Pre-installed

The Copilot coding agent's environment includes:

### Development Tools
- **.NET SDK** (version pinned in `global.json`) - For building the Blazor Web App and Azure Functions
- **Node.js 22** - For running Playwright tests and npm tools
- **Azure Functions Core Tools v4** - For running the Functions API locally
- **Azurite** - Azure Storage Emulator for local blob storage (no Azure subscription needed)

### Project Dependencies
- All .NET packages (via `dotnet restore` and `dotnet build`)
- All Node.js test dependencies (via `npm ci`)
- Playwright browser (Chromium) with system dependencies

## Why This Matters

### Faster Agent Performance
By pre-installing tools and dependencies, the Copilot coding agent:
- Starts working immediately without trial-and-error installation
- Avoids LLM non-determinism when discovering dependencies
- Can reliably run builds, tests, and linters

### No Firewall Issues
All services run locally in the GitHub Actions runner:
- Azurite provides local Azure Storage emulation
- No external Azure connections needed during development
- Functions and Web App connect to local Azurite instance

### Consistent with CI/CD
The environment matches the `e2e-tests.yml` workflow exactly:
- Same .NET SDK (both resolve `global.json`)
- Same Node.js version (22)
- Same Azure Functions Core Tools version (v4)
- Same Azurite setup

This means the agent can reliably run the same tests that run in CI.

## How It Works

1. **Agent Starts Task**: When you ask Copilot to work on the repository
2. **Setup Steps Run**: GitHub Actions executes `copilot-setup-steps.yml`
3. **Environment Ready**: Agent receives a fully configured environment
4. **Agent Works**: Agent can immediately build, test, and modify code

The setup process takes ~5-10 minutes but happens automatically before the agent starts working.

## Workflow Structure

The workflow follows GitHub's required format:

```yaml
jobs:
  copilot-setup-steps:  # MUST be named exactly this
    runs-on: ubuntu-latest
    permissions:
      contents: read  # Minimal permissions for checkout
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
      # ... additional setup steps
```

### Key Components

**Triggers**: The workflow can be triggered by:
- Manual dispatch (for testing)
- Push to the workflow file (for validation)
- Pull requests modifying the workflow file
- Automatically by GitHub Copilot coding agent

**Timeout**: Set to 30 minutes to allow sufficient time for all installations

**Permissions**: Set to minimal `contents: read` for security

**Caching**: Node.js dependencies are cached to speed up subsequent runs

## Testing the Configuration

You can manually test the setup workflow:

1. Go to the repository's "Actions" tab on GitHub
2. Select "Copilot Setup Steps" workflow
3. Click "Run workflow"
4. Monitor the execution to ensure all steps complete successfully

The workflow also runs automatically when:
- You push changes to `copilot-setup-steps.yml`
- You create a PR that modifies `copilot-setup-steps.yml`

## Customization Guidelines

You can customize the following in the `copilot-setup-steps` job:
- `steps` - Add or modify installation steps
- `permissions` - Adjust as needed (keep minimal)
- `runs-on` - Use larger runners if needed (Ubuntu x64 only)
- `services` - Add service containers if needed
- `timeout-minutes` - Adjust timeout (max 59 minutes)

**Do NOT customize**:
- Job name (must be `copilot-setup-steps`)
- `fetch-depth` in checkout (overridden by Copilot)

## Environment Variables

To set environment variables for the Copilot coding agent:

1. Go to repository Settings → Environments
2. Select or create the `copilot` environment
3. Add secrets or variables as needed

For this repository, no additional environment variables are required since:
- Azurite uses well-known development keys
- No real Azure credentials needed for local testing

## Architecture

```
GitHub Copilot Coding Agent
          ↓
    GitHub Actions Runner
    (copilot-setup-steps.yml)
          ↓
    ┌──────────────────────┐
    │ Pre-installed Tools  │
    ├──────────────────────┤
    │ • .NET (global.json) │
    │ • Node.js 22         │
    │ • Azure Functions    │
    │ • Azurite            │
    │ • Playwright         │
    └──────────────────────┘
          ↓
    Agent can immediately:
    • Build solution
    • Run tests
    • Start local services
    • Make code changes
```

## Benefits

### For Developers
- Agent has same environment as your local dev setup
- Consistent behavior between manual and agent work
- No surprises with "works for agent but not for me"

### For CI/CD
- Agent tests changes before committing
- Same tools as the `e2e-tests.yml` workflow
- Early detection of build/test issues

### For Security
- No external dependencies during agent work
- Azurite provides isolated storage
- Minimal permissions in setup steps

## Comparison with Other Environments

| Environment | Purpose | Runs On | Setup Time |
|------------|---------|---------|------------|
| **Copilot Agent** | Agent's workspace | GitHub Actions | ~5-10 min (auto) |
| **Codespaces** | Cloud development | GitHub Codespaces | ~5-10 min (auto) |
| **Dev Container** | Local development | Docker (local) | ~5-10 min (auto) |
| **Manual Setup** | Traditional local | Your machine | Hours (manual) |

All environments use the same tools and versions for consistency.

## Troubleshooting

### Setup Steps Fail

If the workflow fails:
1. Check the Actions tab for error logs
2. Verify all URLs are accessible (packages.microsoft.com, npmjs.org)
3. Ensure the workflow file is on the default branch
4. Check for syntax errors in the YAML

### Agent Can't Run Tests

If the agent reports test failures:
1. Verify the setup workflow completed successfully
2. Check that all dependencies were installed
3. Ensure Playwright browsers were installed with `--with-deps`
4. Review agent logs for specific error messages

### Environment Variables Missing

If the agent needs credentials:
1. Add them to the `copilot` environment in Settings
2. Use secrets for sensitive values
3. Reference them in the workflow if needed during setup

## Further Reading

- [GitHub Copilot Coding Agent Documentation](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent)
- [Customizing Agent Environment](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/customize-the-agent-environment)
- [GitHub Actions Workflow Syntax](https://docs.github.com/en/actions/using-workflows/workflow-syntax-for-github-actions)

## Support

For issues with the Copilot agent environment:
1. Test the workflow manually in Actions tab
2. Review setup step logs
3. Check [TESTING.md](TESTING.md) for test-specific guidance
4. Open an issue if problems persist

---

**The Copilot coding agent is ready to work with this repository! 🤖 ✅**
