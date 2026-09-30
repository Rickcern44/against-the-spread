#!/bin/bash

# Against The Spread - Stop Local Development Services

echo "🛑 Stopping Against The Spread Local Development Environment..."
echo ""

# Stop Azurite
if lsof -Pi :10000 -sTCP:LISTEN -t >/dev/null ; then
    echo "📦 Stopping Azurite..."
    kill $(lsof -t -i:10000)
    echo "✅ Azurite stopped"
else
    echo "ℹ️  Azurite not running"
fi

# Stop Azure Functions
if lsof -Pi :7071 -sTCP:LISTEN -t >/dev/null ; then
    echo "⚡ Stopping Azure Functions..."
    kill $(lsof -t -i:7071)
    echo "✅ Azure Functions stopped"
else
    echo "ℹ️  Azure Functions not running"
fi

# Stop Web App
if lsof -Pi :5158 -sTCP:LISTEN -t >/dev/null ; then
    echo "🌐 Stopping Blazor Web App..."
    kill $(lsof -t -i:5158)
    echo "✅ Blazor Web App stopped"
else
    echo "ℹ️  Blazor Web App not running"
fi

echo ""
echo "✨ All services stopped!"
