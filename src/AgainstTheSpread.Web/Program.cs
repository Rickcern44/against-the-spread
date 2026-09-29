using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AgainstTheSpread.Web;
using AgainstTheSpread.Web.Services;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Fixtures;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The UI stays entirely on this deterministic seam until the integration phase wires the real client.
builder.Services.AddScoped<IAppApiClient, FakeApiClient>();
builder.Services.AddMudServices();
builder.Services.AddScoped<RosterState>();

await builder.Build().RunAsync();
