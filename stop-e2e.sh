#!/bin/bash
# Stop all E2E test services

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "🛑 Stopping E2E test services..."

# Kill SWA CLI
pkill -f "swa start" 2>/dev/null && echo "✓ Stopped SWA CLI" || echo "SWA CLI not running"

# Kill Blazor Web App (be careful not to kill other dotnet processes)
pkill -f "AgainstTheSpread.Web.dll" 2>/dev/null && echo "✓ Stopped Blazor Web App" || echo "Blazor Web App not running"

# Restore original Development config if backup exists
if [ -f "$SCRIPT_DIR/src/AgainstTheSpread.Web/wwwroot/appsettings.Development.json.bak" ]; then
    mv "$SCRIPT_DIR/src/AgainstTheSpread.Web/wwwroot/appsettings.Development.json.bak" \
       "$SCRIPT_DIR/src/AgainstTheSpread.Web/wwwroot/appsettings.Development.json"
    echo "✓ Restored original Development config"
fi

# Kill Azure Functions
pkill -f "func host start" 2>/dev/null && echo "✓ Stopped Azure Functions" || echo "Azure Functions not running"
pkill -f "func start" 2>/dev/null || true

# Kill Azurite
pkill -f "azurite" 2>/dev/null && echo "✓ Stopped Azurite" || echo "Azurite not running"

echo "✅ All E2E services stopped"
