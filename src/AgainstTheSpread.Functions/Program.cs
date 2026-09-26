using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using AgainstTheSpread.Functions.Authentication;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = FunctionsApplication.CreateBuilder(args);

// Aspire ServiceDefaults: OpenTelemetry (OTLP exporter to the local Aspire dashboard when
// running under `aspire start`), service discovery, and resilient HttpClient defaults. Layers
// onto - does not replace - the Azure Monitor exporter registered below.
builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

var otelBuilder = builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

// UseAzureMonitorExporter() throws at startup if no connection string is configured -
// there's no Application Insights resource wired into the local Aspire AppHost, so this
// must stay opt-in via the same env var Azure sets when the Functions app is deployed.
var appInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    otelBuilder.UseAzureMonitorExporter();
}

// Register application services
builder.Services.AddSingleton<IExcelService, ExcelService>();
builder.Services.AddSingleton<IBowlExcelService, BowlExcelService>();
builder.Services.AddSingleton<IGoogleIdTokenValidator, GoogleIdTokenValidator>();
builder.Services.AddSingleton<IAdminAuthorizationService, AdminAuthorizationService>();
builder.Services.AddSingleton<IStorageService>(sp =>
{
    // Use AZURE_STORAGE_CONNECTION_STRING for custom storage, fallback to AzureWebJobsStorage for local dev
    var connectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING")
        ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
        ?? "UseDevelopmentStorage=true";
    var excelService = sp.GetRequiredService<IExcelService>();
    var bowlExcelService = sp.GetRequiredService<IBowlExcelService>();
    var logger = sp.GetRequiredService<ILogger<StorageService>>();
    return new StorageService(connectionString, excelService, bowlExcelService, logger);
});

builder.Build().Run();
