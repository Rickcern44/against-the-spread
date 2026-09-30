#!/bin/bash

# Against The Spread - Dev Container Setup Script
# This script is automatically run when the dev container is created

set -e

echo "🏈 Setting up Against The Spread development environment..."

# Install Azure Functions Core Tools
echo "📦 Installing Azure Functions Core Tools..."
wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y azure-functions-core-tools-4

# Install Azurite globally
echo "📦 Installing Azurite (Azure Storage Emulator)..."
npm install -g azurite

# Restore .NET dependencies
echo "📦 Restoring .NET dependencies..."
dotnet restore

# Build the solution
echo "🔨 Building .NET solution..."
dotnet build --configuration Debug

# Install test dependencies
echo "📦 Installing test dependencies..."
cd tests
npm ci

# Install Playwright browsers
echo "🎭 Installing Playwright browsers..."
npx playwright install --with-deps chromium

cd ..

# Create local.settings.json for Functions if it doesn't exist
FUNC_SETTINGS="src/AgainstTheSpread.Functions/local.settings.json"
if [ ! -f "$FUNC_SETTINGS" ]; then
    echo "⚙️  Creating local.settings.json for Azure Functions..."
    cat > "$FUNC_SETTINGS" << 'EOF'
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
    "AZURE_STORAGE_CONNECTION_STRING": "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}
EOF
fi

# Make scripts executable
chmod +x start-local.sh 2>/dev/null || true
chmod +x stop-local.sh 2>/dev/null || true
chmod +x validate-environment.sh 2>/dev/null || true

echo ""
echo "✅ Dev container setup complete!"
echo ""
echo "📚 Quick Start Guide:"
echo "   • Start all services: ./start-local.sh"
echo "   • Stop all services: ./stop-local.sh"
echo "   • Run tests: cd tests && npm test"
echo "   • Run headed tests: cd tests && npm run test:headed"
echo ""
echo "🔗 Service URLs:"
echo "   • Blazor Web App: http://localhost:5158"
echo "   • Azure Functions: http://localhost:7071"
echo "   • Azurite Storage: http://localhost:10000"
echo ""
