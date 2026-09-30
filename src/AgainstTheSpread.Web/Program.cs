using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AgainstTheSpread.Web;
using AgainstTheSpread.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base URL from configuration
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// Register the API HttpClient for making API calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ApiService>();

// Register services with their own HttpClient instances for web app base address
// These are scoped services so HttpClient will be disposed when the scope ends
builder.Services.AddScoped<ITeamLogoService>(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    var logger = sp.GetRequiredService<ILogger<TeamLogoService>>();
    return new TeamLogoService(logger, httpClient);
});

builder.Services.AddScoped<ITeamColorService>(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    var logger = sp.GetRequiredService<ILogger<TeamColorService>>();
    return new TeamColorService(logger, httpClient);
});

await builder.Build().RunAsync();
