var builder = DistributedApplication.CreateBuilder(args);

// Azure Storage, emulated locally via Azurite. Pinned to the ports the repo's docs/scripts
// already document (10000/10001/10002) and kept persistent so data survives AppHost restarts,
// matching the behavior of scripts/start-local.sh today (Azurite data in /tmp/azurite).
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
    {
        azurite.WithBlobPort(10000)
            .WithQueuePort(10001)
            .WithTablePort(10002)
            .WithLifetime(ContainerLifetime.Persistent);
    });

var blobs = storage.AddBlobs("blobs");

// Azure Functions isolated-worker API. Uses the referenced-project overload since the AppHost
// project-references AgainstTheSpread.Functions.csproj directly.
//
// Host storage: reuse the same emulated "storage" account as the Functions host storage
// (checkpoints/leases) instead of letting Aspire spin up a second implicit storage account -
// this matches local dev today, where a single Azurite instance backs everything.
//
// Connection wiring: AgainstTheSpread.Functions/Program.cs reads a plain
// AZURE_STORAGE_CONNECTION_STRING env var (falling back to AzureWebJobsStorage) and hands it to
// StorageService's constructor, which only accepts a raw connection string - there's no
// Uri+credential or BlobServiceClient overload. Rather than touching that constructor, map
// Aspire's emulator-only blobs connection string onto the exact env var name the app already
// reads via WithEnvironment + ConnectionStringExpression (WithReference alone would inject
// BLOBS_BLOBENDPOINT/BLOBS_CONNECTIONSTRING under Aspire's own naming convention, which nothing
// in this codebase reads yet).
//
// AgainstTheSpread.Functions now calls builder.AddServiceDefaults() itself (via a project
// reference to Aspire.ServiceDefaults) after migrating Program.cs off the classic HostBuilder
// bootstrap onto FunctionsApplication.CreateBuilder(args). Nothing changes here in the AppHost:
// ServiceDefaults is consumed by the Functions project, not orchestrated by the AppHost. Still no
// WithExternalHttpEndpoints() - Functions is only consumed locally by the SWA CLI proxy and the
// Blazor app, never directly external.
// Port: deliberately NOT pinned to 7071. AddAzureFunctionsProject wires its own internal
// "http" endpoint straight to the actual func-host launch port; layering a manual
// WithHttpEndpoint(port: 7071, targetPort: 7071) on top only half-overwrites that (the
// declared endpoint moves to 7071 but the real process keeps whatever port Aspire actually
// assigned it), so DCP ends up proxying to a dead port. Let Aspire assign it dynamically -
// swa's args below resolve the real address live via functions.GetEndpoint("http"), so
// nothing downstream depends on 7071 being the literal port.
var functions = builder.AddAzureFunctionsProject<Projects.AgainstTheSpread_Functions>("functions")
    .WithHostStorage(storage)
    .WithReference(blobs)
    .WaitFor(blobs)
    .WithEnvironment("AZURE_STORAGE_CONNECTION_STRING", blobs.Resource.ConnectionStringExpression);

// Blazor WASM PWA, served by its dev server. Pinned to 5158 - the SWA CLI proxy, Playwright,
// and CI all hardcode this port today, and this app is a WebAssembly entry point (no
// server-side host builder), so ServiceDefaults fundamentally does not apply to it either.
//
// launchProfileName: "http" - AddProject<T> otherwise auto-discovers every profile in the
// project's launchSettings.json, including "https" (a fixed :7103 from the IIS Express
// defaults). That endpoint bypasses the SWA CLI proxy entirely and Aspire will happily
// auto-launch a browser tab on it, which only ever talks to the Blazor static files with no
// path to the API - any real fetch from that tab fails CORS against the :4280 proxy. Pinning
// the profile to "http" keeps Aspire from ever registering or opening that dead-end endpoint.
var web = builder.AddProject<Projects.AgainstTheSpread_Web>("web", launchProfileName: "http")
    .WithReference(functions)
    .WaitFor(functions)
    .WithHttpEndpoint(port: 5158, name: "http")
    // Demote this raw endpoint in the dashboard: it's WASM served with no API proxy in front,
    // so any real API call from a tab opened here always fails CORS against :4280. Keep it out
    // of the prominent summary link (DetailsOnly) and label it so the "swa" resource below -
    // the one actual working entry point - is what people click instead.
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Blazor dev server (no API proxy - do not open directly, use swa's :4280 instead)";
        url.DisplayLocation = UrlDisplayLocation.DetailsOnly;
    });

// Static Web Apps CLI - fronts both web (app) and functions (api) behind a single origin at
// :4280, exactly like scripts/start-e2e.sh / start-local.sh do today. No first-party Aspire
// integration exists for the SWA CLI (Tier 3 raw executable is correct here). Endpoint URLs are
// threaded in via WithArgs + endpoint references rather than hardcoded strings, so the SWA CLI
// always points at whatever host/port Aspire actually bound web/functions to.
//
// --api-devserver-url, not --api-location: `swa start --help` documents --api-location as
// "the folder containing the source code of the API application" (a path) - passing it a URL
// makes the CLI silently skip the API and hang forever with no further output. The documented
// flag for "connect to the api server at this URL instead of using api location" is
// --api-devserver-url (confirmed by live-testing both against a running functions/web pair on
// this machine). scripts/start-e2e.sh has the same --api-location-with-a-URL bug - worth fixing
// there too, tracked separately from this AppHost change.
var swa = builder.AddExecutable("swa", "swa", "..", "start")
    // Proxyless: swa's own --port 4280 arg below means the process binds this exact port
    // itself. isProxied: false is required here - Aspire refuses port == targetPort on a
    // non-container resource when a proxy is requested (it would try to double-bind 4280,
    // once for Aspire's DCP proxy and once for the swa process, and fail to create).
    .WithHttpEndpoint(port: 4280, targetPort: 4280, name: "http", isProxied: false)
    .WithUrlForEndpoint("http", url => url.DisplayText = "Open the app here")
    .WithArgs(context =>
    {
        context.Args.Add(web.GetEndpoint("http"));
        context.Args.Add("--api-devserver-url");
        context.Args.Add(functions.GetEndpoint("http"));
        context.Args.Add("--port");
        context.Args.Add("4280");
    })
    .WithExternalHttpEndpoints()
    .WaitFor(web)
    .WaitFor(functions);

builder.Build().Run();
