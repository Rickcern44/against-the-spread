# Quick Start - Copilot Development Environment

This guide helps you quickly set up a complete development environment for the Against The Spread application using GitHub Copilot.

## GitHub Copilot Coding Agent Environment

**For GitHub Copilot coding agent users:** The repository includes a pre-configured environment setup at `.github/workflows/copilot-setup-steps.yml`. This workflow automatically prepares the agent's environment with:
- .NET SDK (version pinned in `global.json`)
- Node.js 22
- Azure Functions Core Tools v4
- Azurite (Azure Storage Emulator)
- Playwright with Chromium browser
- All project dependencies

When GitHub Copilot coding agent works on this repository, it will automatically use this configuration to set up its ephemeral development environment. No additional setup is required - the agent will have everything it needs to build, test, and run the application locally with Azurite.

## Option 1: GitHub Codespaces (Recommended - No Local Setup)

**Fastest way to get started - runs entirely in the cloud!**

1. **Open in Codespaces:**
   - Go to the repository on GitHub
   - Click the green "Code" button
   - Select "Codespaces" tab
   - Click "Create codespace on main" (or your branch)

2. **Wait for setup (5-10 minutes):**
   - The environment will automatically install all tools
   - .NET (SDK pinned in `global.json`), Node.js, Azure Functions, Azurite, Playwright
   - All dependencies and test browsers

3. **Start developing:**
   ```bash
   # Start all services
   ./start-local.sh
   
   # Open http://localhost:5158 in the browser
   # Codespaces will automatically forward ports
   ```

4. **Run tests:**
   ```bash
   cd tests
   npm test
   ```

**✅ No firewall issues - everything runs in GitHub's infrastructure!**

## Option 2: VS Code Dev Container (Local Docker)

**Best for offline development or when you need more control.**

### Prerequisites
- Docker Desktop installed and running
- VS Code with "Dev Containers" extension
- Git

### Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/quaz579/against-the-spread.git
   cd against-the-spread
   ```

2. **Open in VS Code:**
   ```bash
   code .
   ```

3. **Reopen in Container:**
   - VS Code will show a notification
   - Click "Reopen in Container"
   - Or: F1 → "Dev Containers: Reopen in Container"

4. **Wait for setup (first time takes 5-10 minutes)**

5. **Start developing:**
   ```bash
   ./start-local.sh
   ```

## What's Included

Both options provide:

### Pre-installed Tools
- ✅ .NET SDK (version pinned in `global.json`)
- ✅ Node.js 22
- ✅ Azure Functions Core Tools v4
- ✅ Azurite (Azure Storage Emulator)
- ✅ Playwright with Chromium browser
- ✅ Azure CLI
- ✅ GitHub CLI

### VS Code Extensions
- ✅ C# Dev Kit
- ✅ Azure Functions
- ✅ Playwright Test
- ✅ GitHub Copilot
- ✅ GitHub Copilot Chat

### Pre-configured Services
- ✅ Azurite configured for local storage
- ✅ Azure Functions pointing to Azurite
- ✅ All necessary ports forwarded
- ✅ Environment variables set up

### Built & Ready
- ✅ .NET solution restored and built
- ✅ Test dependencies installed
- ✅ Playwright browsers installed
- ✅ local.settings.json created

## Running the Application

### Start All Services
```bash
./start-local.sh
```

This starts:
- 🗄️ Azurite on port 10000
- ⚡ Azure Functions on port 7071
- 🌐 Blazor Web App on port 5158

### Stop All Services
```bash
./stop-local.sh
```

### Access the Application
Open in your browser: **http://localhost:5158**

(In Codespaces, click the "Ports" tab and click the URL for port 5158)

## Running Tests

### Playwright End-to-End Tests
```bash
cd tests
npm test                    # Run all tests
npm run test:headed        # Run with visible browser
npm run test:debug         # Debug tests
npm run test:report        # View test report
```

### .NET Unit Tests
```bash
dotnet test
```

## Development Workflow

1. **Make code changes** in VS Code
2. **Services auto-reload** (hot reload enabled)
3. **Run tests** to validate changes
4. **Commit and push** when ready

## Troubleshooting

### Ports Already in Use

```bash
# Stop all services and restart
./stop-local.sh
./start-local.sh
```

### Tests Failing

```bash
# Rebuild everything
dotnet clean
dotnet build
cd tests
npm ci
npx playwright install chromium
```

### Dev Container Won't Start

**For Codespaces:**
- Delete the codespace and create a new one
- Go to GitHub → Settings → Codespaces → Delete

**For Local Dev Container:**
- Command Palette (F1) → "Dev Containers: Rebuild Container"
- Check Docker is running: `docker ps`
- Ensure Docker has 4GB+ memory

## Why This Solves Firewall Issues

### Traditional Setup Problems:
- ❌ Corporate firewalls block npm/NuGet downloads
- ❌ Azure service endpoints may be blocked
- ❌ Installing tools requires admin rights
- ❌ Port forwarding issues with VPNs

### Copilot Environment Solutions:
- ✅ **Codespaces:** Runs in GitHub's cloud (no local firewall)
- ✅ **Dev Container:** Isolated Docker network (bypasses most restrictions)
- ✅ **Azurite:** Local emulator (no Azure connection needed)
- ✅ **Pre-installed:** All tools included (no downloads during dev)

## Testing Against Azurite

The Playwright tests use Azurite (Azure Storage Emulator):

1. **No Azure subscription needed** for local testing
2. **No internet connection** required (after initial setup)
3. **Fast reset** - just restart Azurite for clean state
4. **Identical API** - works exactly like real Azure Storage

### How It Works:

```
┌─────────────────┐
│  Playwright     │
│  Tests          │
└────────┬────────┘
         │
         ▼
┌─────────────────┐       ┌─────────────────┐
│  Blazor Web     │◄─────►│  Azure          │
│  (localhost)    │       │  Functions      │
└─────────────────┘       │  (localhost)    │
                          └────────┬────────┘
                                   │
                                   ▼
                          ┌─────────────────┐
                          │  Azurite        │
                          │  (localhost)    │
                          └─────────────────┘

Everything runs locally - no firewall issues!
```

## Next Steps

- Read `.devcontainer/README.md` for detailed documentation
- Check `TESTING.md` for test strategies
- Review `README.md` for architecture overview

## Support

If you encounter issues:
1. Check `.devcontainer/README.md` troubleshooting section
2. Review GitHub Actions workflow (`.github/workflows/e2e-tests.yml`)
3. Open an issue on GitHub

---

**Happy Coding with GitHub Copilot! 🏈 🤖**
