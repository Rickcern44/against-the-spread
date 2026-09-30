# Against The Spread - Development Container

This development container provides a complete, pre-configured environment for developing and testing the Against The Spread application.

## What's Included

### Tools & Runtimes
- **.NET SDK** - version pinned in `global.json`; for building the Blazor app and Azure Functions
- **Node.js 22** - For running Playwright tests and Azurite
- **Azure Functions Core Tools v4** - For running the Functions API locally
- **Azurite** - Azure Storage Emulator for local blob storage
- **Azure CLI** - For managing Azure resources
- **GitHub CLI** - For working with GitHub from the terminal

### VS Code Extensions
- C# Dev Kit
- Azure Functions
- Playwright Test for VS Code
- GitHub Copilot & Copilot Chat

### Pre-configured Services
All necessary ports are automatically forwarded:
- **5158** - Blazor Web App (Frontend)
- **7071** - Azure Functions API (Backend)
- **10000** - Azurite Blob Storage
- **10001** - Azurite Queue Storage
- **10002** - Azurite Table Storage
- **4280** - Azure Static Web Apps CLI (optional)

### Environment Variables
The container automatically sets up:
- `AZURE_STORAGE_CONNECTION_STRING` - Points to local Azurite
- `AzureWebJobsStorage` - For Azure Functions to use Azurite
- `FUNCTIONS_WORKER_RUNTIME` - Configured for .NET isolated worker

## Getting Started

### Prerequisites
- **Docker** installed and running
- **Visual Studio Code** with the "Dev Containers" extension
- **Git** for cloning the repository

### Opening the Dev Container

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
   - VS Code will detect the `.devcontainer` configuration
   - Click the notification "Reopen in Container"
   - Or use Command Palette (F1) → "Dev Containers: Reopen in Container"

4. **Wait for setup:**
   - First-time setup takes 5-10 minutes
   - The container will install all dependencies automatically
   - You'll see progress in the terminal

### What Happens During Setup

The setup script (`.devcontainer/setup.sh`) automatically:
1. ✅ Installs Azure Functions Core Tools
2. ✅ Installs Azurite (Azure Storage Emulator)
3. ✅ Restores .NET dependencies
4. ✅ Builds the .NET solution
5. ✅ Installs Node.js test dependencies
6. ✅ Installs Playwright browsers
7. ✅ Creates `local.settings.json` for Azure Functions

## Running the Application Locally

### Quick Start - All Services

Use the provided helper script to start everything:

```bash
./start-local.sh
```

This starts:
- 🗄️ **Azurite** (Storage Emulator) on port 10000
- ⚡ **Azure Functions** (API) on port 7071
- 🌐 **Blazor Web App** (Frontend) on port 5158

Access the app at: **http://localhost:5158**

### Stop All Services

```bash
./stop-local.sh
```

### Manual Service Management

If you prefer to run services individually:

**1. Start Azurite (Terminal 1):**
```bash
azurite --location /tmp/azurite --blobPort 10000 --silent
```

**2. Start Azure Functions (Terminal 2):**
```bash
cd src/AgainstTheSpread.Functions
func start
```

**3. Start Blazor Web App (Terminal 3):**
```bash
cd src/AgainstTheSpread.Web
dotnet run --urls http://localhost:5158
```

## Running Tests

### Playwright Smoke Tests

The Playwright tests validate the complete user flow with local services.

**Run all tests:**
```bash
cd tests
npm test
```

**Run with visible browser (headed mode):**
```bash
cd tests
npm run test:headed
```

**Debug tests interactively:**
```bash
cd tests
npm run test:debug
```

**View test report:**
```bash
cd tests
npm run test:report
```

### .NET Unit Tests

```bash
dotnet test
```

### Test Prerequisites

The Playwright tests require:
- ✅ Azurite running on port 10000
- ✅ Azure Functions running on port 7071
- ✅ Blazor Web App running on port 5158
- ✅ Test data uploaded to Azurite

**The tests automatically:**
1. Start all services during global setup
2. Upload test data (Week 11 & 12) to Azurite
3. Run the user flow tests
4. Generate reports and traces

## Troubleshooting

### Port Already in Use

If you see "address already in use" errors:

```bash
# Find and kill process using a port (e.g., 7071)
lsof -ti:7071 | xargs kill -9
```

Or restart the dev container:
- Command Palette (F1) → "Dev Containers: Rebuild Container"

### Tests Timeout or Fail

1. **Ensure all services are running:**
   ```bash
   # Check if services are responding
   curl http://localhost:7071/api/weeks?year=2025
   curl http://localhost:5158
   ```

2. **Check service logs:**
   - Look for errors in the terminal where services are running
   - Functions should show "Functions host started"
   - Web app should show "Now listening on: http://localhost:5158"

3. **Rebuild and restart:**
   ```bash
   dotnet clean
   dotnet build
   ./stop-local.sh
   ./start-local.sh
   ```

### Azurite Issues

If Azurite has connection issues:

```bash
# Stop Azurite
pkill azurite

# Clear Azurite data
rm -rf /tmp/azurite

# Restart Azurite
azurite --location /tmp/azurite --blobPort 10000 --silent &
```

### Dev Container Won't Start

1. **Check Docker is running:**
   ```bash
   docker ps
   ```

2. **Rebuild the container:**
   - Command Palette (F1) → "Dev Containers: Rebuild Container"

3. **Check Docker resources:**
   - Ensure Docker has enough memory (at least 4GB)
   - Check disk space

## Development Workflow

### Making Code Changes

1. **Edit code** in VS Code
2. **Hot reload** is enabled for:
   - Blazor app (`dotnet watch` in Web project)
   - Azure Functions (automatic reload in `func host`)
3. **Run tests** to validate changes
4. **Commit and push** when ready

### Running Specific Tests

```bash
# Run specific Playwright test file
cd tests
npx playwright test specs/full-flow.spec.ts

# Run .NET tests for a specific project
dotnet test src/AgainstTheSpread.Tests/AgainstTheSpread.Tests.csproj
```

### Debugging

**Debugging .NET Code:**
1. Set breakpoints in VS Code
2. Press F5 or use "Run and Debug" panel
3. Select "Functions" or "Web App" configuration

**Debugging Playwright Tests:**
1. Open test file in VS Code
2. Click the green play button next to test name
3. Or use Playwright VS Code extension

## CI/CD Integration

The dev container matches the CI environment configured in `.github/workflows/e2e-tests.yml`:

- Same .NET SDK (both resolve `global.json`)
- Same Node.js version (22)
- Same Azure Functions Core Tools (v4)
- Same Azurite version
- Same Playwright version

This ensures **tests work the same locally and in CI**.

## Customization

### Adding VS Code Extensions

Edit `.devcontainer/devcontainer.json`:
```json
{
  "customizations": {
    "vscode": {
      "extensions": [
        "your.extension.id"
      ]
    }
  }
}
```

### Installing Additional Tools

Edit `.devcontainer/setup.sh` and add commands to install tools.

### Changing Port Mappings

Edit `.devcontainer/devcontainer.json`:
```json
{
  "forwardPorts": [
    5158,
    7071,
    10000
  ]
}
```

## Resource Links

- [Dev Containers Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [Azure Functions Core Tools](https://docs.microsoft.com/azure/azure-functions/functions-run-local)
- [Azurite Documentation](https://docs.microsoft.com/azure/storage/common/storage-use-azurite)
- [Playwright Documentation](https://playwright.dev/)

## Support

If you encounter issues:
1. Check the [main README](../README.md)
2. Review [TESTING.md](../TESTING.md) for test-specific help
3. Open an issue on GitHub
4. Check the troubleshooting section above

---

**Happy Coding! 🏈**
