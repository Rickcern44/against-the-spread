using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Standard Aspire "ServiceDefaults" pattern (matching the `dotnet new aspire-servicedefaults`
// template), adapted for consumption by an isolated-worker Azure Functions host.
//
// NOTE: this project intentionally does NOT expose a `MapDefaultEndpoints(WebApplication)`
// extension. That method requires a `WebApplication`/`IEndpointRouteBuilder` so it can map
// `/health` and `/alive` GET endpoints directly. An Azure Functions isolated-worker host built
// via `FunctionsApplication.CreateBuilder(args)` + `ConfigureFunctionsWebApplication()` calls
// `.Build()` to get a plain `IHost`, not a `WebApplication` - there is no endpoint-routing
// pipeline the Functions host exposes to application code to hang extra endpoints off of.
// Per Microsoft's own Aspire + Azure Functions integration guide
// (https://learn.microsoft.com/en-us/azure/azure-functions/aspire-integration), the recommended
// minimal Program.cs for a Functions project calls only `AddServiceDefaults()` +
// `ConfigureFunctionsWebApplication()` - no `MapDefaultEndpoints()` call appears anywhere in that
// guidance. Health-check *registration* (`AddDefaultHealthChecks` below) is still useful here:
// it participates in DI and could be surfaced by a future admin/health-check HTTP trigger if
// this app ever needs one, but there is no automatic endpoint to map.
public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        // These calls layer onto the SAME underlying OpenTelemetry SDK registration that
        // AgainstTheSpread.Functions/Program.cs also configures via
        // `services.AddOpenTelemetry().UseFunctionsWorkerDefaults().UseAzureMonitorExporter()`.
        // `IServiceCollection.AddOpenTelemetry()` is idempotent/cumulative - calling it more than
        // once returns the same OpenTelemetryBuilder wrapping the same TracerProvider/
        // MeterProviderBuilder rather than creating a second, competing pipeline. That lets this
        // method own the OTLP exporter (for the local Aspire dashboard) while the Functions
        // project's own registration keeps exporting to Azure Monitor when actually deployed -
        // both destinations stay wired up side by side instead of one replacing the other.
        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            // Only wired up when the Aspire AppHost injects OTEL_EXPORTER_OTLP_ENDPOINT (local
            // dev under `aspire start`). When running standalone (func start, or deployed to
            // Azure), this is a no-op and the Azure Monitor exporter registered in Program.cs is
            // the only exporter attached.
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }
}
