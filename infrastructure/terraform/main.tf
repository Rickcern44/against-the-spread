terraform {
  required_version = ">= 1.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}

  subscription_id = var.subscription_id
}

# Variables
variable "subscription_id" {
  description = "Azure subscription ID. Locally: copy terraform.tfvars.example to terraform.tfvars (gitignored). In CI: set ARM_SUBSCRIPTION_ID from a repository secret and leave this unset."
  type        = string
  default     = null
}

variable "project_name" {
  description = "Project name for resource naming"
  type        = string
  default     = "against-the-spread"
}

variable "environment" {
  description = "Environment (dev, staging, prod)"
  type        = string
  default     = "prod"
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "eastus"
}

# Locals
locals {
  resource_prefix = "${var.project_name}-${var.environment}"
  tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${local.resource_prefix}-rg"
  location = var.location
  tags     = local.tags
}

# Storage Account for game files and function storage
resource "azurerm_storage_account" "main" {
  name                     = replace("${local.resource_prefix}st", "-", "")
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  blob_properties {
    cors_rule {
      allowed_headers    = ["*"]
      allowed_methods    = ["GET", "HEAD", "POST", "PUT"]
      allowed_origins    = ["*"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }

  tags = local.tags
}

# Container for game files (lines)
resource "azurerm_storage_container" "gamefiles" {
  name                  = "gamefiles"
  storage_account_name  = azurerm_storage_account.main.name
  container_access_type = "private"
}

# Application Insights for monitoring
resource "azurerm_application_insights" "main" {
  name                = "${local.resource_prefix}-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  application_type    = "web"
  tags                = local.tags
}

# App Service Plan for Azure Functions
resource "azurerm_service_plan" "main" {
  name                = "${local.resource_prefix}-asp"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = "Y1" # Consumption plan
  tags                = local.tags
}

# Function App
resource "azurerm_linux_function_app" "main" {
  name                = "${local.resource_prefix}-func"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.main.id

  storage_account_name       = azurerm_storage_account.main.name
  storage_account_access_key = azurerm_storage_account.main.primary_access_key

  site_config {
    application_stack {
      dotnet_version              = "9.0"
      use_dotnet_isolated_runtime = true
    }

    cors {
      allowed_origins = ["*"] # Update in production with specific origins
    }
  }

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"       = "dotnet-isolated"
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.main.instrumentation_key
    "AzureWebJobsStorage"            = azurerm_storage_account.main.primary_connection_string
    "WEBSITE_RUN_FROM_PACKAGE"       = "1"
  }

  tags = local.tags
}

# Static Web App for Blazor
resource "azurerm_static_web_app" "main" {
  name                = "${local.resource_prefix}-web"
  location            = "eastus2" # Static Web Apps have limited regions
  resource_group_name = azurerm_resource_group.main.name
  sku_tier            = "Free"
  sku_size            = "Free"
  tags                = local.tags
}

# Outputs
output "resource_group_name" {
  value       = azurerm_resource_group.main.name
  description = "Resource group name"
}

output "storage_account_name" {
  value       = azurerm_storage_account.main.name
  description = "Storage account name for admin uploads"
}

output "storage_connection_string" {
  value       = azurerm_storage_account.main.primary_connection_string
  description = "Storage connection string (sensitive)"
  sensitive   = true
}

output "function_app_name" {
  value       = azurerm_linux_function_app.main.name
  description = "Function app name"
}

output "function_app_url" {
  value       = "https://${azurerm_linux_function_app.main.default_hostname}"
  description = "Function app URL"
}

output "static_web_app_url" {
  value       = "https://${azurerm_static_web_app.main.default_host_name}"
  description = "Static web app URL"
}

output "static_web_app_deployment_token" {
  value       = azurerm_static_web_app.main.api_key
  description = "Deployment token for Static Web App"
  sensitive   = true
}
