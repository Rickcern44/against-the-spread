using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Services;

public class StarterRecommendationServiceTests
{
    private readonly IStarterRecommendationService _service = new StarterRecommendationService();

    [Fact]
    public void Recommend_ReproducesWeek3GoldenOrderingAndTiebreak()
    {
        var roster = RecommendationFixtureLoader.LoadRoster();
        var games = RecommendationFixtureLoader.LoadWeek3Games();

        var result = _service.Recommend(roster, games, week: 3);

        result.Ranking.Select(c => c.TeamId).Should().Equal("DET", "NO", "KC", "NYG", "SF", "CIN", "MIN", "BAL", "LAR");
        result.Ranking.Select(c => c.WinProbability).Should().Equal(0.698m, 0.6023m, 0.7706m, 0.5735m, 0.7107m, 0.6023m, 0.4558m, 0.5879m, 0.4558m);
        result.Ranking.Select(c => c.ExpectedValue).Should().Equal(2.7918m, 2.4091m, 2.3117m, 2.2938m, 2.1322m, 1.8068m, 1.3673m, 1.1759m, 0.9115m);
        result.ByeTeams.Should().BeEmpty();

        // docs/swan-league-spec.md §7.6: the method doc's worked example (49ers over Saints,
        // 48.9%/38.6%) does not reproduce from the spread - Saints lead 49ers by 0.2769 EV, far
        // outside the tiebreak band, so no tiebreak fires between that pair at all. The real
        // contested third slot, recomputed from the pinned conversion function, is Chiefs vs.
        // Giants: this is the invariant under test, not the method doc's literal figures.
        result.Tiebreak.Triggered.Should().BeTrue();
        result.Tiebreak.CandidateTeamIds.Should().BeEquivalentTo(["KC", "NYG"]);
        result.Tiebreak.EvGap.Should().Be(0.0179m);
        result.Tiebreak.PerfectWeekProbability["KC"].Should().Be(0.324m);
        result.Tiebreak.PerfectWeekProbability["NYG"].Should().Be(0.2411m);
        result.Tiebreak.WinnerTeamId.Should().Be("KC");

        result.RecommendedStarters.Should().Equal("DET", "NO", "KC");
    }

    [Fact]
    public void Recommend_ExcludesByeTeamsFromRanking()
    {
        var roster = Roster(("A", 4, 3), ("B", 3, null), ("C", 2, null), ("D", 1, null), ("E", 1, null), ("F", 1, null), ("G", 1, null), ("H", 1, null), ("I", 1, null));
        var games = new[]
        {
            Game("b-game", "B", "OPP1", 3m),
            Game("c-game", "C", "OPP2", 3m),
            Game("d-game", "D", "OPP3", 3m),
        };

        var result = _service.Recommend(roster, games, week: 3);

        result.ByeTeams.Should().Equal("A");
        result.Ranking.Select(c => c.TeamId).Should().NotContain("A");
    }

    [Fact]
    public void Recommend_NeverBenchesBothSidesOfAnIntraRosterCollision()
    {
        var roster = Roster(("FAV", 4, null), ("DOG", 4, null), ("C", 2, null), ("D", 1, null), ("E", 1, null), ("F", 1, null), ("G", 1, null), ("H", 1, null), ("I", 1, null));
        var games = new[]
        {
            Game("collision", "FAV", "DOG", 7m),
            Game("c-game", "C", "OPP2", 3m),
        };

        var result = _service.Recommend(roster, games, week: 3);

        var teamIds = result.Ranking.Select(c => c.TeamId).ToList();
        teamIds.Should().Contain("FAV");
        teamIds.Should().NotContain("DOG");
    }

    private static SeasonRoster Roster(params (string Id, int Value, int? Bye)[] teams) =>
        new(2026, teams.Select(t => new Team(t.Id, t.Id, 5 - t.Value, t.Bye ?? 18)));

    private static WeeklyGame Game(string id, string favorite, string underdog, decimal line) =>
        new(id, 3, favorite, underdog, line, null);
}
