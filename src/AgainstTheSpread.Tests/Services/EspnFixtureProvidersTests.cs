using AgainstTheSpread.Core.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Services;

public sealed class EspnFixtureProvidersTests
{
    [Fact]
    public async Task Lines_fixture_normalizes_favorite_and_underdog_without_assuming_home_is_favorite()
    {
        await using var stream = File.OpenRead(Fixture("odds-espn-2026-week3.json"));
        var games = await new EspnFixtureLinesProvider(stream).GetWeeklyGamesAsync(2026, 3);

        games.Should().HaveCount(16);
        games.Single(x => x.Id == "401872963").Should().BeEquivalentTo(
            new AgainstTheSpread.Core.Models.WeeklyGame("401872963", 3, "PHI", "CHI", 3.5m, null));
        games.Single(x => x.Id == "401872952").UnderdogTeamId.Should().Be("MIA");
    }

    [Fact]
    public async Task Results_fixture_returns_only_final_games()
    {
        await using var stream = File.OpenRead(Fixture("scores-espn-2026-week3.json"));
        var results = await new EspnFixtureResultsProvider(stream).GetFinalResultsAsync(2026, 3);

        results.Should().HaveCount(15);
        results.Should().NotContain(x => x.GameId == "401872963"); // PHI @ CHI was still scheduled.
        results.Should().OnlyContain(x => x.HomeTeamId.Length > 0 && x.AwayTeamId.Length > 0);
    }

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
