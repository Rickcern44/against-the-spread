using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Tests.Web;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace AgainstTheSpread.Tests.Web.Pages;

/// <summary>Covers the "This week" checklist slice: roster-limited starters, the full dog
/// slate with its declared allowance, ET lock states, post-deadline corrections, and the
/// "Assumed line" label on an unconfirmed ESPN spread.</summary>
public sealed class PicksTests : MudBunitTestContext
{
    private static readonly DateTimeOffset BeforeStarterDeadline = new(2026, 9, 12, 12, 0, 0, TimeSpan.FromHours(-4));
    private static readonly DateTimeOffset AfterStarterDeadline = new(2026, 9, 13, 14, 0, 0, TimeSpan.FromHours(-4));

    private async Task<FakeApiClient> BuildRosteredClientAsync()
    {
        var client = new FakeApiClient();
        // Only three of this week's eight slate teams (PHI, KC, BUF) are on the roster; the rest
        // of the nine are bench teams with no game this week, so the starter picker must exclude
        // DAL/LAC/BAL/DET/GB even though they're all in the week's full games/teams payload.
        var teamIds = new[] { "PHI", "KC", "BUF", "CLE", "ARI", "ATL", "CAR", "CHI", "CIN" };
        await client.SaveSeasonRosterAsync(new SaveSeasonRosterRequest(teamIds));
        return client;
    }

    [Fact]
    public async Task Picks_StarterPicker_IsLimitedToRosterTeams()
    {
        var client = await BuildRosteredClientAsync();
        Services.AddScoped<IAppApiClient>(_ => client);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(BeforeStarterDeadline));

        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Starters"));
        var startersRows = cut.FindAll("#starters-section + div.picker-list > div.picker-row");
        // Exactly the nine roster teams appear as pickable starter rows (even the six on bye
        // this week), never the other slate teams (Dallas/LAC/Baltimore/Detroit/Green Bay).
        startersRows.Should().HaveCount(9);
        var startersSection = cut.Find("#starters-section + div.picker-list").OuterHtml;
        startersSection.Should().Contain("Philadelphia Eagles").And.Contain("Kansas City Chiefs").And.Contain("Buffalo Bills");
        // Detroit and Green Bay play each other, and neither is on the roster, so they never
        // appear in the starters section even as an opponent mention — unlike Dallas/LAC/
        // Baltimore, which are legitimately named there as roster teams' opponents.
        startersSection.Should().NotContain("Detroit Lions").And.NotContain("Green Bay Packers");

        // The dog section, by contrast, shows the full eligible slate regardless of roster.
        cut.Markup.Should().Contain("Dallas Cowboys").And.Contain("Baltimore Ravens");
    }

    [Fact]
    public async Task Picks_DogAllowance_BlocksAnotherSelectionOnceReached()
    {
        var client = await BuildRosteredClientAsync();
        Services.AddScoped<IAppApiClient>(_ => client);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(BeforeStarterDeadline));

        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Dog picks"));

        var dogButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Select dog")).ToList();
        dogButtons.Should().HaveCountGreaterThanOrEqualTo(2);

        dogButtons[0].Click();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Selected — remove"));

        var remainingDogButton = cut.FindAll("button").First(b => b.TextContent.Contains("Select dog"));
        remainingDogButton.Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Dog allowance reached (1)"));
    }

    [Fact]
    public async Task Picks_AfterStarterDeadline_ShowsLockedStateAndOffersCorrection()
    {
        var client = await BuildRosteredClientAsync();
        Services.AddScoped<IAppApiClient>(_ => client);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(AfterStarterDeadline));

        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("starter deadline has passed"));
        cut.Markup.Should().Contain("Correct my record");

        // Starter rows are shown read-only (disabled), not silently actionable as on-time picks.
        var startersSection = cut.Find("#starters-section + div.picker-list").OuterHtml;
        startersSection.Should().Contain("disabled");
    }

    [Fact]
    public async Task Picks_CorrectionAfterDeadline_SavesAndRecalculatesScore()
    {
        var client = await BuildRosteredClientAsync();
        var recalculating = new RecalculatingApiClient(client);
        Services.AddScoped<IAppApiClient>(_ => recalculating);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(AfterStarterDeadline));

        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("starter deadline has passed"));

        var correctButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Correct my record"));
        correctButton.Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("personal correction"));

        // Save with whatever the (empty-by-default for week 1's pre-saved pick, already-complete)
        // draft currently holds — week 1 ships with a saved pick, so Save is already enabled.
        var saveButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Save my record"));
        saveButton.Click();

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Personal correction saved"));
        cut.Markup.Should().Contain("does not change the commissioner's submission");
        recalculating.StandingsCallCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Picks_UnconfirmedEspnLine_ShowsAssumedLineLabel()
    {
        var client = await BuildRosteredClientAsync();
        Services.AddScoped<IAppApiClient>(_ => client);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(BeforeStarterDeadline));

        var cut = Render<AgainstTheSpread.Web.Pages.Picks>();

        // KC-LAC carries only an ESPN line (OfficialLine is null in the fixture), so its dog row
        // must carry the "Assumed line" label; DAL-PHI has a confirmed OfficialLine and must not.
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Assumed line"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    /// <summary>Wraps <see cref="FakeApiClient"/> to simulate the server recalculating the
    /// week's running score after a save, and counts standings reads for the test to assert on.</summary>
    private sealed class RecalculatingApiClient(FakeApiClient inner) : IAppApiClient
    {
        public int StandingsCallCount { get; private set; }
        private WeeklyScore? recalculated;

        public Task<ApiResponse<WeekGamesResponse>> GetWeekGamesAsync(int week, CancellationToken ct = default) => inner.GetWeekGamesAsync(week, ct);
        public Task<ApiResponse<WeekPicksResponse>> GetWeekPicksAsync(int week, CancellationToken ct = default) => inner.GetWeekPicksAsync(week, ct);
        public Task<ApiResponse<WeekRecommendationResponse>> GetWeekRecommendationAsync(int week, CancellationToken ct = default) => inner.GetWeekRecommendationAsync(week, ct);
        public Task<ApiResponse<WeekDataStatusResponse>> GetWeekDataAsync(int week, CancellationToken ct = default) => inner.GetWeekDataAsync(week, ct);
        public Task<ApiResponse<WeekDataStatusResponse>> PostWeekDataAsync(int week, PostWeekDataRequest request, CancellationToken ct = default) => inner.PostWeekDataAsync(week, request, ct);
        public Task<ApiResponse<WeekDetailResponse>> GetWeekDetailAsync(int week, CancellationToken ct = default) => inner.GetWeekDetailAsync(week, ct);
        public Task<ApiResponse<SeasonRosterResponse>> GetSeasonRosterAsync(CancellationToken ct = default) => inner.GetSeasonRosterAsync(ct);
        public Task<ApiResponse<SeasonRosterResponse>> SaveSeasonRosterAsync(SaveSeasonRosterRequest request, CancellationToken ct = default) => inner.SaveSeasonRosterAsync(request, ct);

        public async Task<ApiResponse<WeekPicksResponse>> SaveWeekPicksAsync(int week, SaveWeekPicksRequest request, CancellationToken ct = default)
        {
            var result = await inner.SaveWeekPicksAsync(week, request, ct);
            recalculated = new WeeklyScore(99m, 3m, 6m, new PoolTotals(99m, 6m, 102m, 0m), new TierWinCounts(Green: 3), true, true, false, null, true, false, new[] { 1 });
            return result;
        }

        public async Task<ApiResponse<StandingsResponse>> GetStandingsAsync(CancellationToken ct = default)
        {
            StandingsCallCount++;
            var baseline = await inner.GetStandingsAsync(ct);
            if (recalculated is null || !baseline.Succeeded) return baseline;
            var history = baseline.Data!.WeeklyHistory.Select(h => h.Week == 1 ? h with { Score = recalculated } : h).ToList();
            return ApiResponse<StandingsResponse>.Success(baseline.Data! with { WeeklyHistory = history });
        }
    }
}
