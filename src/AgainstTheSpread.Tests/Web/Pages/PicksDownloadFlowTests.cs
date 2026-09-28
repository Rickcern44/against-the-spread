using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Pages;

public sealed class PicksDownloadFlowTests : BunitContext
{
    public PicksDownloadFlowTests() => Services.AddScoped<IAppApiClient, FakeApiClient>();

    [Fact]
    public void Picks_ShowsRankedRecommendationAndSavedActualPicks()
    {
        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();
        cut.Markup.Should().Contain("This week’s ranked recommendation");
        cut.Markup.Should().Contain("Philadelphia Eagles");
        cut.Find("#starter-0").Change("21");
        cut.Find("#starter-1").Change("12");
        cut.Find("#starter-2").Change("2");
        cut.Find("#dog").Change("6");
        cut.Find("button[type=submit]").Click();
        cut.Markup.Should().Contain("Your picks are saved.");
        cut.Markup.Should().Contain("Running score:");
    }

    [Fact]
    public void Picks_ShowsWeekNotPulledState()
    {
        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();
        cut.Find("#week").Change("2");
        cut.Markup.Should().Contain("has not been pulled");
    }
}
