using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Pages;

/// <summary>Covers the "History" week-browsing slice: a scorable past week renders its saved
/// starters/dogs and score, and always surfaces the honest correction-history gap message,
/// since persisted correction timestamps don't exist yet.</summary>
public sealed class HistoryTests : MudBunitTestContext
{
    [Fact]
    public void History_DefaultsToWeekOne_ShowsSavedPicksScoreAndCorrectionGapMessage()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();

        var cut = Render<AgainstTheSpread.Web.Pages.History>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Saved starters"));
        cut.Markup.Should().Contain("Philadelphia Eagles");
        cut.Markup.Should().Contain("Week 1 score:");
        cut.Markup.Should().Contain("isn't persisted yet");
    }

    [Fact]
    public void History_WeekWithNoStoredSlate_ShowsWeekNotPulledMessage()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();

        var cut = Render<AgainstTheSpread.Web.Pages.History>(parameters => parameters.Add(p => p.Week, 5));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("has not been pulled"));
    }
}
