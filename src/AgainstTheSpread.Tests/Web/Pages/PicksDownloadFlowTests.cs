using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace AgainstTheSpread.Tests.Web.Pages;

public sealed class PicksDownloadFlowTests : MudBunitTestContext
{
    public PicksDownloadFlowTests() => Services.AddScoped<IAppApiClient, FakeApiClient>();

    [Fact]
    public async Task Picks_ShowsRankedRecommendationAndSavedActualPicks()
    {
        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();
        cut.Markup.Should().Contain("This week's ranked recommendation");
        cut.Markup.Should().Contain("Philadelphia Eagles");

        // MudSelect renders its option list into a JS-managed popover, which isn't portaled into
        // this component's own render tree in bUnit. Drive the selects the same way a click on a
        // rendered option would — by invoking the component's bound ValueChanged callback — rather
        // than fighting the popover DOM.
        var starterSelects = cut.FindComponents<MudSelect<string>>();
        await cut.InvokeAsync(() => starterSelects[0].Instance.ValueChanged.InvokeAsync("21"));
        await cut.InvokeAsync(() => starterSelects[1].Instance.ValueChanged.InvokeAsync("12"));
        await cut.InvokeAsync(() => starterSelects[2].Instance.ValueChanged.InvokeAsync("2"));
        await cut.InvokeAsync(() => starterSelects[3].Instance.ValueChanged.InvokeAsync("6"));

        cut.Find("button").Click();

        cut.Markup.Should().Contain("Your picks are saved.");
        cut.Markup.Should().Contain("Running score:");
    }

    [Fact]
    public async Task Picks_ShowsWeekNotPulledState()
    {
        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();
        var weekSelect = cut.FindComponent<MudSelect<int>>();
        await cut.InvokeAsync(() => weekSelect.Instance.ValueChanged.InvokeAsync(2));
        cut.Markup.Should().Contain("has not been pulled");
    }
}
