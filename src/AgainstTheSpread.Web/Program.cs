using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AgainstTheSpread.Web;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Fixtures;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The UI stays entirely on this deterministic seam until the integration phase wires the real client.
builder.Services.AddScoped<IAppApiClient, FakeApiClient>();

await builder.Build().RunAsync();
