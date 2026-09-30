# Azure Deployment Guide

## Quick Start Deployment

This guide will help you deploy the Against The Spread application to Azure in under 10 minutes.

## Prerequisites

1. Azure account with active subscription
2. Azure CLI installed (run: `brew install azure-cli`)
3. Logged into Azure (run: `az login`)

## Step 1: Login to Azure

```bash
az login
```

## Step 2: Create Azure Static Web App + Functions

Run this single command to create everything:

```bash
# Set your variables
PROJECT_NAME="against-the-spread"
LOCATION="eastus"
RESOURCE_GROUP="${PROJECT_NAME}-rg"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create Static Web App (includes Functions support)
az staticwebapp create \
  --name $PROJECT_NAME \
  --resource-group $RESOURCE_GROUP \
  --source https://github.com/quaz579/against-the-spread \
  --location $LOCATION \
  --branch main \
  --app-location "src/AgainstTheSpread.Web" \
  --api-location "src/AgainstTheSpread.Functions" \
  --output-location "bin/Release/net9.0/publish/wwwroot" \
  --login-with-github
```

This will:
- Create a resource group
- Create an Azure Static Web App
- Connect it to your GitHub repository
- Set up automatic deployments
- Configure the Blazor app and Azure Functions

## Step 3: Create Storage Account

Your app needs Azure Storage for game files:

```bash
STORAGE_ACCOUNT="${PROJECT_NAME}storage$(openssl rand -hex 4)"

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Get connection string
CONNECTION_STRING=$(az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv)

# Create container for game files
az storage container create \
  --name gamefiles \
  --account-name $STORAGE_ACCOUNT \
  --connection-string "$CONNECTION_STRING"

echo "Storage Account: $STORAGE_ACCOUNT"
echo "Connection String: $CONNECTION_STRING"
```

## Step 4: Configure Application Settings

Add the storage connection string to your Static Web App:

```bash
# Get the Static Web App name
STATIC_WEB_APP_NAME=$(az staticwebapp list \
  --resource-group $RESOURCE_GROUP \
  --query "[0].name" \
  --output tsv)

# Set application settings (for Functions)
az staticwebapp appsettings set \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --setting-names AzureWebJobsStorage="$CONNECTION_STRING"
```

## Step 5: Get Deployment Token and Add to GitHub

```bash
# Get deployment token
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.apiKey \
  --output tsv)

echo "Deployment Token: $DEPLOYMENT_TOKEN"

# Add to GitHub secrets using gh CLI
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$DEPLOYMENT_TOKEN"

# Also set the storage connection string as a secret
gh secret set AZURE_STORAGE_CONNECTION_STRING --body "$CONNECTION_STRING"
```

## Step 6: Trigger Deployment

```bash
# Trigger GitHub Actions workflow
gh workflow run deploy.yml

# Watch the deployment
gh run watch
```

## Step 7: Get Your App URL

```bash
az staticwebapp show \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query defaultHostname \
  --output tsv
```

Your app will be available at: `https://<name>.azurestaticapps.net`

## One-Command Deployment Script

Save this as `deploy-to-azure.sh` and run it:

```bash
#!/bin/bash
set -e

# Variables
PROJECT_NAME="against-the-spread"
LOCATION="eastus"
RESOURCE_GROUP="${PROJECT_NAME}-rg"
STORAGE_ACCOUNT="${PROJECT_NAME}st$(openssl rand -hex 4)"

echo "🚀 Deploying Against The Spread to Azure..."

# Login check
if ! az account show &>/dev/null; then
    echo "Please login to Azure first:"
    az login
fi

# Create resource group
echo "📦 Creating resource group..."
az group create --name $RESOURCE_GROUP --location $LOCATION --output none

# Create Static Web App
echo "🌐 Creating Static Web App..."
az staticwebapp create \
  --name $PROJECT_NAME \
  --resource-group $RESOURCE_GROUP \
  --source https://github.com/quaz579/against-the-spread \
  --location $LOCATION \
  --branch main \
  --app-location "src/AgainstTheSpread.Web" \
  --api-location "src/AgainstTheSpread.Functions" \
  --output-location "bin/Release/net9.0/publish/wwwroot" \
  --login-with-github \
  --output none

# Create storage account
echo "💾 Creating storage account..."
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --output none

# Get connection string
CONNECTION_STRING=$(az storage account show-connection-string \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --query connectionString \
  --output tsv)

# Create container
az storage container create \
  --name gamefiles \
  --account-name $STORAGE_ACCOUNT \
  --connection-string "$CONNECTION_STRING" \
  --output none

# Get Static Web App name
STATIC_WEB_APP_NAME=$(az staticwebapp list \
  --resource-group $RESOURCE_GROUP \
  --query "[0].name" \
  --output tsv)

# Configure app settings
echo "⚙️  Configuring application settings..."
az staticwebapp appsettings set \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --setting-names AzureWebJobsStorage="$CONNECTION_STRING" \
  --output none

# Get deployment token
DEPLOYMENT_TOKEN=$(az staticwebapp secrets list \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query properties.apiKey \
  --output tsv)

# Add secrets to GitHub
echo "🔐 Adding secrets to GitHub..."
gh secret set AZURE_STATIC_WEB_APPS_API_TOKEN --body "$DEPLOYMENT_TOKEN"
gh secret set AZURE_STORAGE_CONNECTION_STRING --body "$CONNECTION_STRING"

# Get app URL
APP_URL=$(az staticwebapp show \
  --name $STATIC_WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query defaultHostname \
  --output tsv)

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📊 Resource Group: $RESOURCE_GROUP"
echo "💾 Storage Account: $STORAGE_ACCOUNT"
echo "🌐 App URL: https://$APP_URL"
echo ""
echo "GitHub Actions will now automatically deploy your app."
echo "Watch the deployment: gh run watch"
```

## Next Steps

1. Go to your app URL
2. Upload weekly lines at `/admin`
3. Users can make picks and download Excel files

## Troubleshooting

### Check deployment status
```bash
gh run list --workflow=deploy.yml
```

### View logs
```bash
gh run view --log
```

### Check Static Web App status
```bash
az staticwebapp show --name $PROJECT_NAME --resource-group $RESOURCE_GROUP
```

### Test Functions locally
```bash
./start-local.sh
```

## Cost

- **Azure Static Web Apps**: Free tier available (100 GB bandwidth/month)
- **Azure Storage**: ~$0.02/GB/month
- **Total**: Less than $5/month for moderate usage

## Cleanup

To delete all resources:

```bash
az group delete --name against-the-spread-rg --yes --no-wait
```
