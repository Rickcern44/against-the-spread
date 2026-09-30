using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Pages;

public sealed class SettingsTests : MudBunitTestContext
{
    [Fact]
    public void Settings_NoRosterSaved_ShowsEmptyPicker()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();

        var cut = Render<AgainstTheSpread.Web.Pages.Settings>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("0 / 9 selected"));
    }

    [Fact]
    public async Task Settings_RosterAlreadySaved_PreselectsExistingTeams()
    {
        var client = new FakeApiClient();
        var teamIds = NflTeamDirectory.AllTeams.Take(9).Select(t => t.Id).ToList();
        await client.SaveSeasonRosterAsync(new SaveSeasonRosterRequest(teamIds));
        Services.AddScoped<IAppApiClient>(_ => client);

        var cut = Render<AgainstTheSpread.Web.Pages.Settings>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("9 / 9 selected"));
    }
}
