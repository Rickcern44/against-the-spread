#!/bin/bash
# Start all services needed for local E2E testing
# This script starts Azurite, Azure Functions, Blazor Web App (with E2E config), and SWA CLI

set -e

echo "🚀 Starting E2E test environment..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Get the directory where this script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Function to check if a port is in use
check_port() {
    lsof -i :$1 >/dev/null 2>&1
}

# 1. Start Azurite (Azure Storage Emulator)
echo -e "${YELLOW}Starting Azurite...${NC}"
if check_port 10000; then
    echo -e "${GREEN}✓ Azurite is already running on port 10000${NC}"
else
    mkdir -p /tmp/azurite
    azurite --silent --location /tmp/azurite &
    sleep 2
    if check_port 10000; then
        echo -e "${GREEN}✓ Azurite started successfully${NC}"
    else
        echo -e "${RED}✗ Failed to start Azurite${NC}"
        exit 1
    fi
fi

# 2. Start Azure Functions
echo -e "${YELLOW}Starting Azure Functions...${NC}"
if check_port 7071; then
    echo -e "${YELLOW}⚠️  Azure Functions is already running on port 7071${NC}"
else
    cd "$SCRIPT_DIR/src/AgainstTheSpread.Functions"
    # Export ADMIN_EMAILS for E2E testing
    export ADMIN_EMAILS="test-admin@example.com"
    func start --dotnet-isolated --port 7071 > /tmp/func-e2e.log 2>&1 &
    cd "$SCRIPT_DIR"
    
    # Wait for Functions to be ready
    echo "Waiting for Azure Functions to start..."
    for i in {1..30}; do
        if check_port 7071; then
            echo -e "${GREEN}✓ Azure Functions started successfully${NC}"
            break
        fi
        sleep 1
    done
    
    if ! check_port 7071; then
        echo -e "${RED}✗ Failed to start Azure Functions${NC}"
        cat /tmp/func-e2e.log
        exit 1
    fi
fi

# 3. Start Blazor Web App with E2E configuration
echo -e "${YELLOW}Starting Blazor Web App (E2E mode)...${NC}"
if check_port 5158; then
    echo -e "${YELLOW}⚠️  Blazor Web App is already running on port 5158${NC}"
else
    cd "$SCRIPT_DIR/src/AgainstTheSpread.Web"
    # Backup Development config and use E2E config (points API to SWA CLI port 4280)
    if [ -f wwwroot/appsettings.Development.json ]; then
        cp wwwroot/appsettings.Development.json wwwroot/appsettings.Development.json.bak
        cp wwwroot/appsettings.E2E.json wwwroot/appsettings.Development.json
        echo "  (Using E2E config: API → localhost:4280)"
    fi
    dotnet run --urls "http://localhost:5158" > /tmp/web-e2e.log 2>&1 &
    cd "$SCRIPT_DIR"
    
    # Wait for Web App to be ready
    echo "Waiting for Blazor Web App to start..."
    for i in {1..30}; do
        if check_port 5158; then
            echo -e "${GREEN}✓ Blazor Web App started successfully${NC}"
            break
        fi
        sleep 1
    done
    
    if ! check_port 5158; then
        echo -e "${RED}✗ Failed to start Blazor Web App${NC}"
        cat /tmp/web-e2e.log
        exit 1
    fi
fi

# 4. Start SWA CLI (Static Web Apps CLI for mock authentication)
echo -e "${YELLOW}Starting SWA CLI...${NC}"
if check_port 4280; then
    echo -e "${YELLOW}⚠️  SWA CLI is already running on port 4280${NC}"
else
    swa start http://localhost:5158 --api-location http://localhost:7071 --port 4280 > /tmp/swa-e2e.log 2>&1 &
    
    # Wait for SWA CLI to be ready
    echo "Waiting for SWA CLI to start..."
    for i in {1..30}; do
        if check_port 4280; then
            echo -e "${GREEN}✓ SWA CLI started successfully${NC}"
            break
        fi
        sleep 1
    done
    
    if ! check_port 4280; then
        echo -e "${RED}✗ Failed to start SWA CLI${NC}"
        cat /tmp/swa-e2e.log
        exit 1
    fi
fi

echo ""
echo -e "${GREEN}✅ E2E test environment is ready!${NC}"
echo ""
echo "Services running:"
echo "  - Azurite (Storage):     http://localhost:10000"
echo "  - Azure Functions (API): http://localhost:7071"
echo "  - Blazor Web App:        http://localhost:5158"
echo "  - SWA CLI (Entry Point): http://localhost:4280"
echo ""
echo "To run E2E tests: cd tests && npm test"
echo "To stop services: ./stop-e2e.sh"
