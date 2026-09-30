# Copilot Environment Setup - Complete Guide

This document explains the Copilot development environment setup for the Against The Spread application.

## What Was Created

### 1. Development Container Configuration (`.devcontainer/`)

The dev container provides a complete, isolated development environment that works with:
- **GitHub Codespaces** - Cloud-based development environment
- **VS Code Dev Containers** - Local Docker-based development

#### Files Created:
- **`devcontainer.json`** - Main configuration for the dev container
- **`setup.sh`** - Automated setup script that runs when container is created
- **`README.md`** - Comprehensive documentation for the dev container
- **`codespaces.md`** - GitHub Codespaces specific notes

### 2. Helper Scripts

#### `validate-environment.sh`
A comprehensive validation script that tests:
- ✅ Azurite (Azure Storage Emulator) starts correctly
- ✅ .NET solution builds successfully
- ✅ Azure Functions API starts and responds
- ✅ Blazor Web App starts and responds
- ✅ Playwright test dependencies are installed
- ✅ Playwright browsers are installed

**Usage:**
```bash
./validate-environment.sh
```

#### Updated `stop-local.sh`
Fixed to stop services on the correct port (5158 instead of 5000).

### 3. Documentation

#### `COPILOT_ENVIRONMENT.md`
A quick start guide that explains:
- Two setup options (Codespaces vs Dev Container)
- Step-by-step setup instructions
- How to run the application
- How to run tests
- Troubleshooting common issues
- Why this solves firewall problems

#### Updated `README.md`
Added a prominent section at the top linking to the Copilot environment setup.

## Key Features

### Pre-installed Tools
The dev container includes all necessary tools:
- **.NET SDK** (version pinned in `global.json`) - For building Blazor and Azure Functions
- **Node.js 22** - For Playwright tests and tooling
- **Azure Functions Core Tools v4** - For running Functions locally
- **Azurite** - Azure Storage Emulator (no Azure subscription needed)
- **Playwright** - With Chromium browser pre-installed
- **Azure CLI** - For Azure resource management
- **GitHub CLI** - For GitHub operations

### Pre-configured VS Code Extensions
- C# Dev Kit
- Azure Functions
- Playwright Test for VS Code
- GitHub Copilot
- GitHub Copilot Chat

### Automatic Port Forwarding
All necessary ports are automatically forwarded:
- **5158** - Blazor Web App
- **7071** - Azure Functions API
- **10000** - Azurite Blob Storage
- **10001** - Azurite Queue Storage
- **10002** - Azurite Table Storage
- **4280** - SWA CLI (if used)

### Environment Variables
Pre-configured for local development:
- `AZURE_STORAGE_CONNECTION_STRING` - Points to Azurite
- `AzureWebJobsStorage` - For Functions to use Azurite
- `FUNCTIONS_WORKER_RUNTIME` - Set to `dotnet-isolated`

## How It Solves Firewall Issues

### Traditional Local Development Problems:
1. ❌ Corporate firewalls block npm/NuGet package downloads
2. ❌ Azure service endpoints may be blocked
3. ❌ VPNs cause port forwarding issues
4. ❌ Installing tools requires administrator rights
5. ❌ Inconsistent environments between developers

### Copilot Environment Solutions:

#### GitHub Codespaces (Cloud-based):
- ✅ **No firewall issues** - Runs entirely in GitHub's cloud infrastructure
- ✅ **No local installation** - All tools pre-installed in the container
- ✅ **Consistent environment** - Same setup for all developers
- ✅ **No admin rights needed** - Everything runs in the container
- ✅ **Works from anywhere** - Just need a browser

#### VS Code Dev Container (Local Docker):
- ✅ **Isolated network** - Docker containers bypass most firewall rules
- ✅ **Pre-installed tools** - No downloads during development
- ✅ **Reproducible** - Same environment every time
- ✅ **Portable** - Works on Windows, Mac, and Linux

#### Azurite (Storage Emulator):
- ✅ **No Azure connection needed** - Everything runs locally
- ✅ **No subscription required** - Free and open source
- ✅ **Fast reset** - Just restart for clean state
- ✅ **Identical API** - Works exactly like Azure Storage

## Testing Architecture

```
┌─────────────────┐
│  Playwright     │
│  Tests          │
│  (Node.js)      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐       ┌─────────────────┐
│  Blazor Web     │◄─────►│  Azure          │
│  localhost:5158 │       │  Functions      │
│                 │       │  localhost:7071 │
└─────────────────┘       └────────┬────────┘
                                   │
                                   ▼
                          ┌─────────────────┐
                          │  Azurite        │
                          │  localhost:10000│
                          │  (Storage Emu)  │
                          └─────────────────┘

Everything runs locally - no firewall issues!
```

## Quick Start Options

### Option 1: GitHub Codespaces (Recommended)
**Fastest way to get started - zero local setup!**

1. Go to the repository on GitHub
2. Click "Code" → "Codespaces" → "Create codespace"
3. Wait 5-10 minutes for setup to complete
4. Run `./start-local.sh`
5. Open http://localhost:5158

### Option 2: VS Code Dev Container
**Best for offline development**

1. Install Docker Desktop
2. Install VS Code with "Dev Containers" extension
3. Clone the repository
4. Open in VS Code
5. Click "Reopen in Container" when prompted
6. Wait 5-10 minutes for setup
7. Run `./start-local.sh`
8. Open http://localhost:5158

## Running Tests

### Validate Environment
```bash
# Test that all services can start
./validate-environment.sh
```

### Run Playwright Tests
```bash
cd tests
npm test                  # Run all tests
npm run test:headed      # Run with visible browser
npm run test:debug       # Debug tests interactively
npm run test:report      # View HTML report
```

### Run .NET Unit Tests
```bash
dotnet test
```

## What Gets Automatically Set Up

When you open the dev container, the `setup.sh` script automatically:

1. ✅ Installs Azure Functions Core Tools
2. ✅ Installs Azurite globally via npm
3. ✅ Restores all .NET dependencies (`dotnet restore`)
4. ✅ Builds the .NET solution (`dotnet build`)
5. ✅ Installs test dependencies (`npm ci` in tests/)
6. ✅ Installs Playwright browsers (`npx playwright install chromium`)
7. ✅ Creates `local.settings.json` for Azure Functions
8. ✅ Makes startup scripts executable

**Total setup time: 5-10 minutes (first time only)**

## CI/CD Integration

The dev container matches the CI environment exactly:
- Same .NET SDK (both resolve `global.json`)
- Same Node.js version (22)
- Same Azure Functions Core Tools version (v4)
- Same Azurite version
- Same Playwright version

**This means: If tests pass locally, they'll pass in CI!**

## Troubleshooting

### Dev Container Won't Start

**For Codespaces:**
1. Delete the codespace
2. Create a new one
3. Settings → Codespaces → Delete

**For Local Dev Container:**
1. Command Palette (F1)
2. "Dev Containers: Rebuild Container"
3. Check Docker is running: `docker ps`
4. Ensure Docker has 4GB+ memory

### Services Won't Start

```bash
# Stop everything
./stop-local.sh

# Validate environment
./validate-environment.sh

# Start again
./start-local.sh
```

### Tests Fail

```bash
# Rebuild everything
dotnet clean
dotnet build

# Reinstall test dependencies
cd tests
rm -rf node_modules
npm ci
npx playwright install chromium
```

### Port Already in Use

```bash
# Find what's using the port (e.g., 7071)
lsof -i :7071

# Kill it
kill $(lsof -t -i:7071)

# Or use stop script
./stop-local.sh
```

## Maintenance

### Updating Dependencies

**.NET packages:**
```bash
dotnet restore
dotnet build
```

**Node.js packages:**
```bash
cd tests
npm ci
npm audit fix
```

**Playwright browsers:**
```bash
cd tests
npx playwright install chromium
```

### Rebuilding the Dev Container

If you update `.devcontainer/devcontainer.json`:
1. Command Palette (F1)
2. "Dev Containers: Rebuild Container"
3. Wait for rebuild to complete

## Security Considerations

### Azurite Connection String
The Azurite connection string in the environment uses the **well-known development account key**:
```
AccountName: devstoreaccount1
AccountKey: Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==
```

**This is safe because:**
- ✅ It's the standard development key (publicly documented)
- ✅ Only works with Azurite (not real Azure Storage)
- ✅ Only accessible on localhost
- ✅ Never used in production

**Do NOT use this key with real Azure Storage!**

## Benefits Summary

| Feature | Traditional Setup | Copilot Environment |
|---------|------------------|---------------------|
| Setup Time | Hours | 5-10 minutes |
| Firewall Issues | Common | Eliminated |
| Consistency | Varies by dev | Identical for all |
| Azure Subscription | Required | Not needed |
| Works Offline | Partially | Yes (after setup) |
| Admin Rights | Often required | Not needed |
| Tool Installation | Manual | Automatic |
| Environment Conflicts | Possible | Isolated |

## Resources

- [Dev Containers Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [GitHub Codespaces Documentation](https://docs.github.com/codespaces)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite Documentation](https://docs.microsoft.com/azure/storage/common/storage-use-azurite)
- [Playwright Documentation](https://playwright.dev/)

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review `.devcontainer/README.md`
3. Review `COPILOT_ENVIRONMENT.md`
4. Check `TESTING.md` for test-specific help
5. Open an issue on GitHub

---

**Built with ❤️ for seamless development! 🏈 🤖**
