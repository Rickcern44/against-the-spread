using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Pages;

public sealed class MyStatsTests : MudBunitTestContext
{
    [Fact]
    public void Stats_ShowsFourPoolsAndWeeklyHistory()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        var cut = Render<AgainstTheSpread.Web.Pages.MyStats>();
        cut.Markup.Should().Contain("Main").And.Contain("Dog").And.Contain("Regular season").And.Contain("Playoff");
        cut.Markup.Should().Contain("Going Perf:").And.Contain("Week-by-week");
    }

    [Fact]
    public void Stats_ShowsHonestOpponentStandingsEmptyState()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        var cut = Render<AgainstTheSpread.Web.Pages.MyStats>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("League standing"));
        cut.Markup.Should().Contain("Opponent standings aren't available yet");
        cut.Markup.Should().Contain("your own");
    }
}
