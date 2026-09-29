using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AgainstTheSpread.Web.Components;
using AgainstTheSpread.Web.Services;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Components;

/// <summary>
/// Onboarding gate: every route except /welcome must first prove a season roster exists.
/// </summary>
public sealed class RosterGateTests : MudBunitTestContext
{
    public RosterGateTests() => Services.AddScoped<RosterState>();

    [Fact]
    public void NoRosterSaved_RequestingDashboard_RedirectsToWelcome()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        var routeData = new RouteData(typeof(AgainstTheSpread.Web.Pages.Dashboard), new Dictionary<string, object?>());

        var cut = Render<RosterGate>(ps => ps.Add(p => p.RouteData, routeData));

        var navigation = Services.GetRequiredService<NavigationManager>();
        cut.WaitForAssertion(() => navigation.Uri.Should().EndWith("/welcome"));
    }

    [Fact]
    public void NoRosterSaved_RequestingWelcome_RendersOnboardingWithoutRedirecting()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        var routeData = new RouteData(typeof(AgainstTheSpread.Web.Pages.Welcome), new Dictionary<string, object?>());
        var navigation = Services.GetRequiredService<NavigationManager>();
        var startingUri = navigation.Uri;

        var cut = Render<RosterGate>(ps => ps.Add(p => p.RouteData, routeData));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Draft your roster"));
        navigation.Uri.Should().Be(startingUri);
    }

    [Fact]
    public async Task RosterAlreadySaved_RequestingMyStats_RendersRequestedPage()
    {
        var client = new FakeApiClient();
        var teamIds = NflTeamDirectory.AllTeams.Take(9).Select(t => t.Id).ToList();
        await client.SaveSeasonRosterAsync(new SaveSeasonRosterRequest(teamIds));
        Services.AddScoped<IAppApiClient>(_ => client);
        var routeData = new RouteData(typeof(AgainstTheSpread.Web.Pages.MyStats), new Dictionary<string, object?>());

        var cut = Render<RosterGate>(ps => ps.Add(p => p.RouteData, routeData));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Going Perf:"));
    }
}
