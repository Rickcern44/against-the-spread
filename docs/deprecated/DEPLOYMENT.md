# Deployment Guide

This guide explains how to deploy the Against The Spread application to Azure using GitHub Actions and Terraform.

## Prerequisites

- Azure subscription
- Azure CLI installed locally
- Terraform installed locally
- GitHub repository set up
- GitHub Actions enabled

## Step-by-Step Deployment

### 1. Deploy Azure Infrastructure

First, provision all Azure resources using Terraform:

```bash
# Login to Azure
az login

# Navigate to Terraform directory
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Review the plan
terraform plan

# Apply infrastructure
terraform apply
```

Save the outputs - you'll need these values:
```bash
terraform output
```

Key outputs:
- `function_app_name`
- `function_app_url`
- `static_web_app_url`
- `static_web_app_deployment_token`
- `storage_account_name`

### 2. Configure GitHub Secrets

Add the following secrets to your GitHub repository (Settings → Secrets and variables → Actions):

#### Required Secrets:

1. **AZURE_FUNCTION_APP_NAME**
   - Value: Output from `terraform output function_app_name`

2. **AZURE_FUNCTION_APP_PUBLISH_PROFILE**
   - Get from Azure Portal:
     - Navigate to Function App
     - Click "Get publish profile" in Overview
     - Copy the entire XML content

3. **AZURE_STATIC_WEB_APPS_API_TOKEN**
   - Value: Output from `terraform output static_web_app_deployment_token`
   - Or get from Azure Portal → Static Web App → Manage deployment token

### 3. Update Application Configuration

Update the Blazor app to point to your Function App:

```bash
# Edit src/AgainstTheSpread.Web/wwwroot/appsettings.Production.json
{
  "ApiBaseUrl": "https://your-function-app.azurewebsites.net/"
}
```

Replace with your actual Function App URL from Terraform output.

### 4. Trigger Deployment

#### Option A: Push to main branch
```bash
git add .
git commit -m "Configure production settings"
git push origin main
```

GitHub Actions will automatically:
1. Build all projects
2. Run tests
3. Deploy Functions to Azure
4. Deploy Blazor app to Static Web Apps

#### Option B: Manual workflow dispatch
- Go to GitHub → Actions
- Select "CD - Deploy to Azure"
- Click "Run workflow"
- Select branch and run

### 5. Verify Deployment

Check that everything is working:

1. **Functions API**:
   ```bash
   curl https://your-function-app.azurewebsites.net/api/weeks?year=2025
   ```

2. **Static Web App**:
   - Open `https://your-static-web-app.azurestaticapps.net` in browser
   - Should see the home page

### 6. Upload First Week's Lines

Use the admin script to upload weekly lines:

```bash
cd infrastructure/scripts
export STORAGE_ACCOUNT_NAME="your-storage-account-name"
./upload-lines.sh 1 2025 ../../reference-docs/"Week 1 Lines.csv"
```

### 7. Test End-to-End

1. Navigate to your Static Web App URL
2. Click "Make Your Picks"
3. Select Week 1
4. Pick 6 games
5. Download Excel file
6. Verify Excel format matches "Weekly Picks Example.csv"

## CI/CD Workflows

### Continuous Integration (ci.yml)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main`

**Actions:**
1. Restore NuGet packages
2. Build solution
3. Run all tests
4. Publish test results
5. Upload build artifacts

### Continuous Deployment (deploy.yml)

**Triggers:**
- Push to `main` branch
- Manual workflow dispatch

**Jobs:**

1. **deploy-functions**:
   - Build Functions project
   - Deploy to Azure Functions

2. **deploy-static-web-app**:
   - Build Blazor WebAssembly app
   - Deploy to Azure Static Web Apps

## Monitoring and Logs

### Application Insights

View logs and telemetry:
```bash
# Get Application Insights connection string
terraform output -raw appinsights_connection_string

# View in Azure Portal
az portal
# Navigate to Application Insights → your-app-ai
```

### Function App Logs

```bash
# Stream logs
az functionapp log tail --name your-function-app --resource-group your-rg

# Or use Azure Portal
# Function App → Log Stream
```

### Static Web App Logs

- Azure Portal → Static Web Apps → your-app → Application Insights

## Rollback

If deployment fails or there are issues:

### Rollback Functions

```bash
# List deployments
az functionapp deployment list --name your-function-app --resource-group your-rg

# Rollback to previous deployment
az functionapp deployment source config-zip \
  --resource-group your-rg \
  --name your-function-app \
  --src path/to/previous/package.zip
```

### Rollback Static Web App

- GitHub Actions maintains history
- Re-run a previous successful workflow
- Or manually deploy from a previous commit

## Updating Configuration

### Change API URL

1. Update `appsettings.Production.json`
2. Commit and push
3. Deployment will automatically update

### Change CORS Settings

1. Edit `infrastructure/terraform/main.tf`
2. Update `azurerm_linux_function_app.site_config.cors.allowed_origins`
3. Run `terraform apply`

### Scale Function App

Change from Consumption to Premium:

```terraform
# In main.tf
resource "azurerm_service_plan" "main" {
  # ... 
  sku_name = "EP1"  # Change from Y1
}
```

Then:
```bash
terraform apply
```

## Troubleshooting

### Deployment Fails

1. Check GitHub Actions logs
2. Verify all secrets are set correctly
3. Ensure Azure resources exist: `terraform state list`

### Function App Not Responding

```bash
# Restart function app
az functionapp restart --name your-function-app --resource-group your-rg

# Check status
az functionapp show --name your-function-app --resource-group your-rg --query state
```

### Static Web App Shows 404

- Check deployment logs in GitHub Actions
- Verify app_location path in deploy.yml
- Ensure build succeeded

### API Calls Failing from Blazor

1. Check CORS configuration in Function App
2. Verify `ApiBaseUrl` in appsettings.Production.json
3. Check Function App is running: `az functionapp show`

## Cost Optimization

### Development Environment

For dev/test, use cheaper resources:

```terraform
# In terraform/main.tf or use variables
variable "environment" {
  default = "dev"
}

# Static Web App already uses Free tier
# Functions uses Consumption (pay per use)
# Storage is minimal cost
```

### Production Optimizations

1. Enable Azure CDN for Static Web App (if high traffic)
2. Configure blob lifecycle management for old picks
3. Set up budget alerts in Azure Portal

## Maintenance

### Regular Updates

1. **Weekly**: Check Application Insights for errors
2. **Monthly**: Review costs in Azure Portal
3. **Quarterly**: Update NuGet packages and redeploy

### Backup Strategy

The application is stateless except for uploaded lines:

```bash
# Backup storage account
az storage blob download-batch \
  --source gamefiles \
  --destination ./backup \
  --account-name your-storage-account
```

### Disaster Recovery

1. All code in Git → can redeploy from scratch
2. Infrastructure in Terraform → `terraform apply` recreates everything
3. Uploaded lines → regular backups recommended

## Security Best Practices

1. **Secrets Management**:
   - Use GitHub Secrets (not in code)
   - Rotate Azure credentials regularly

2. **CORS**:
   - Update to specific origins in production
   - Don't use `*` for allowed_origins

3. **Authentication** (Future Enhancement):
   - Current MVP has no auth (anonymous picks)
   - Consider adding Azure AD B2C for user accounts

4. **Storage Access**:
   - Blob storage is private
   - Only Functions can access via connection string

## Next Steps After Deployment

1. Monitor Application Insights for first week
2. Test with real users making picks
3. Verify Excel downloads match expected format
4. Set up Azure alerts for failures
5. Document any issues or improvements needed
