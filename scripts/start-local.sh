#!/bin/bash

# Against The Spread - Local Development Startup Script

REPO_ROOT="$( cd "$( dirname "${BASH_SOURCE[0]}" )/.." && pwd )"
cd "$REPO_ROOT"

echo "🏈 Starting Against The Spread Local Development Environment..."
echo ""

# Check if Azurite is already running
if lsof -Pi :10000 -sTCP:LISTEN -t >/dev/null ; then
    echo "⚠️  Azurite is already running on port 10000"
else
    echo "📦 Starting Azurite (Storage Emulator)..."
    mkdir -p /tmp/azurite
    azurite --silent --location /tmp/azurite --blobPort 10000 &
    AZURITE_PID=$!
    echo "✅ Azurite started (PID: $AZURITE_PID)"
fi

echo ""

# Check if Functions are already running
if lsof -Pi :7071 -sTCP:LISTEN -t >/dev/null ; then
    echo "⚠️  Azure Functions is already running on port 7071"
else
    echo "⚡ Starting Azure Functions (Backend API)..."
    cd src/AgainstTheSpread.Functions
    func start &
    FUNC_PID=$!
    cd ../..
    echo "✅ Azure Functions starting (PID: $FUNC_PID)"
fi

echo ""

# Wait a bit for Functions to start
echo "⏳ Waiting for Functions to initialize..."
sleep 5

# Check if Web App is already running
if lsof -Pi :5158 -sTCP:LISTEN -t >/dev/null ; then
    echo "⚠️  Web App is already running on port 5158"
else
    echo "🌐 Starting Blazor Web App (Frontend)..."
    cd src/AgainstTheSpread.Web
    dotnet run &
    WEB_PID=$!
    cd ../..
    echo "✅ Blazor Web App starting (PID: $WEB_PID)"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 Local Development Environment Started!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📍 Services Running:"
echo "   • Storage Emulator: http://localhost:10000"
echo "   • Azure Functions:  http://localhost:7071"
echo "   • Web Application:  http://localhost:5158"
echo ""
echo "🔗 Open in browser: http://localhost:5158"
echo ""
echo "⏹️  To stop all services:"
echo "   ./scripts/stop-local.sh"
echo ""
echo "Press Ctrl+C to view logs, or close terminal to keep running in background"
echo ""

# Keep script running to show logs
wait
