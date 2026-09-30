using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AgainstTheSpread.Web.Services;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Pages;

public sealed class WelcomeTests : MudBunitTestContext
{
    [Fact]
    public void Welcome_RendersRosterPicker()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        Services.AddScoped<RosterState>();

        var cut = Render<AgainstTheSpread.Web.Pages.Welcome>();

        cut.Markup.Should().Contain("Draft your roster");
        cut.Markup.Should().Contain("0 / 9 selected");
    }

    [Fact]
    public void Welcome_SavingNineTeams_NavigatesToDashboard()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        Services.AddScoped<RosterState>();
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = Render<AgainstTheSpread.Web.Pages.Welcome>();
        for (var i = 0; i < 9; i++) cut.FindAll("button.team-tile")[i].Click();
        cut.FindAll("button").Single(b => !b.ClassList.Contains("team-tile")).Click();

        cut.WaitForAssertion(() => navigation.Uri.Should().NotContain("/welcome"));
    }
}
