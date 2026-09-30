#!/bin/bash

# Test Script - Validates the Copilot environment setup
# This script tests that all services can start correctly

set -e

echo "🧪 Testing Copilot Environment Setup..."
echo ""

# Function to kill processes on ports
cleanup() {
    echo "🧹 Cleaning up test processes..."
    pkill -9 azurite 2>/dev/null || true
    pkill -9 func 2>/dev/null || true
    pkill -9 dotnet 2>/dev/null || true
    sleep 2
    echo "✅ Cleanup complete"
}

# Cleanup on exit
trap cleanup EXIT

# Test 1: Start Azurite
echo "1️⃣ Testing Azurite..."
mkdir -p /tmp/azurite-validate
azurite --location /tmp/azurite-validate --blobPort 10000 --silent &
AZURITE_PID=$!
echo "   Started Azurite (PID: $AZURITE_PID)"

# Wait for Azurite to be ready
sleep 3

# Check if Azurite is responding
if curl -s -f http://localhost:10000/ > /dev/null 2>&1 || netstat -tuln | grep -q ":10000 "; then
    echo "   ✅ Azurite is running on port 10000"
else
    echo "   ❌ Azurite failed to start"
    exit 1
fi

# Test 2: Build .NET solution
echo ""
echo "2️⃣ Testing .NET Build..."
# Dynamically determine repo root (works in GitHub Actions, Codespaces, local)
if REPO_ROOT="$(git rev-parse --show-toplevel 2>/dev/null)"; then
    cd "$REPO_ROOT"
else
    # Fallback: use script directory
    cd "$(dirname "$0")"
fi
if dotnet build --no-restore --configuration Release > /dev/null 2>&1; then
    echo "   ✅ .NET solution builds successfully"
else
    echo "   ❌ .NET build failed"
    exit 1
fi

# Test 3: Start Azure Functions
echo ""
echo "3️⃣ Testing Azure Functions..."
cd src/AgainstTheSpread.Functions

# Create local.settings.json if needed
cat > local.settings.json << 'EOF'
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
    "AZURE_STORAGE_CONNECTION_STRING": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
EOF

func start --port 7071 > /tmp/func.log 2>&1 &
FUNC_PID=$!
echo "   Started Azure Functions (PID: $FUNC_PID)"

# Wait for Functions to be ready (up to 30 seconds)
echo "   Waiting for Functions to initialize..."
for i in {1..30}; do
    if curl -s -f http://localhost:7071/api/weeks?year=2025 > /dev/null 2>&1; then
        echo "   ✅ Azure Functions is running on port 7071"
        break
    fi
    sleep 1
    if [ $i -eq 30 ]; then
        echo "   ❌ Azure Functions failed to start within 30 seconds"
        echo "   Last 20 lines of func.log:"
        tail -20 /tmp/func.log
        exit 1
    fi
done

# Test 4: Start Blazor Web App
echo ""
echo "4️⃣ Testing Blazor Web App..."
cd ../AgainstTheSpread.Web
dotnet run --no-build --urls http://localhost:5158 > /tmp/web.log 2>&1 &
WEB_PID=$!
echo "   Started Web App (PID: $WEB_PID)"

# Wait for Web App to be ready (up to 30 seconds)
echo "   Waiting for Web App to initialize..."
for i in {1..30}; do
    if curl -s -f http://localhost:5158 > /dev/null 2>&1; then
        echo "   ✅ Web App is running on port 5158"
        break
    fi
    sleep 1
    if [ $i -eq 30 ]; then
        echo "   ❌ Web App failed to start within 30 seconds"
        echo "   Last 20 lines of web.log:"
        tail -20 /tmp/web.log
        exit 1
    fi
done

# Test 5: Playwright test dependencies
echo ""
echo "5️⃣ Testing Playwright Dependencies..."
cd ../../tests
if [ -d "node_modules/@playwright/test" ]; then
    echo "   ✅ Playwright test dependencies installed"
else
    echo "   ❌ Playwright test dependencies missing"
    exit 1
fi

if [ -d "$HOME/.cache/ms-playwright/chromium-"* ]; then
    echo "   ✅ Playwright browsers installed"
else
    echo "   ❌ Playwright browsers not installed"
    exit 1
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ All Copilot Environment Tests Passed!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "🎉 Your environment is ready for development and testing!"
echo ""
echo "Next steps:"
echo "   • Run: ./start-local.sh   (start all services)"
echo "   • Run: cd tests && npm test   (run Playwright tests)"
echo "   • Open: http://localhost:5158   (view the app)"
echo ""
