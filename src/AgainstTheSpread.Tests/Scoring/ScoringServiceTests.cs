using System.Text.Json;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Scoring;

public class ScoringServiceTests
{
    private readonly ScoringService _service = new();

    [Fact]
    public void ScoreWeek_ReproducesWorkbookGoldenCases()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "expected", "scoring-2026-workbook-cases.json")));
        fixture.RootElement.GetProperty("cases").GetArrayLength().Should().Be(6);

        var rick = Roster(("DET", 4), ("CIN", 3), ("KC", 3), ("SF", 3), ("BAL", 2), ("NYG", 4), ("NO", 4), ("MIN", 3), ("LAR", 2));
        var rickWeek1 = _service.ScoreWeek(rick, Pick(1, ["DET", "CIN", "KC"], ("d1", "DOG")),
            [Game("det", 1, "DET", 4, true), Game("cin", 1, "CIN", 3, true), Game("kc", 1, "KC", 3, true), DogGame("d1", 1, 3, true)]);
        AssertGolden(rickWeek1, 10, 3, 3, 13, 16, true, true, false, 16, new(1, 2));

        var rickWeek2 = _service.ScoreWeek(rick, Pick(2, ["KC", "SF", "BAL"], ("d2", "DOG")),
            [Game("kc", 2, "KC", 3, true), Game("sf", 2, "SF", 3, true), Game("bal", 2, "BAL", 2, false), DogGame("d2", 2, 6, false)]);
        AssertGolden(rickWeek2, 6, 0, 0, 6, 6, false, false, false, 17, new(0, 2));
        var rickSeason = _service.ScoreSeason([rickWeek1, rickWeek2], []);
        rickSeason.Pools.Should().Be(new PoolTotals(22, 3, 19, 0));

        var sam = Roster(("NYG", 4), ("DET", 4), ("LV", 4), ("KC", 3), ("SF", 3), ("LAR", 2), ("A", 1), ("B", 1), ("C", 1));
        var samWeek1 = _service.ScoreWeek(sam, Pick(1, ["NYG", "DET", "LV"], ("sd1", "DOG")),
            [Game("nyg", 1, "NYG", 4, true), Game("det", 1, "DET", 4, true), Game("lv", 1, "LV", 4, true), DogGame("sd1", 1, 6, false)]);
        AssertGolden(samWeek1, 12, 3, 0, 15, 15, true, false, false, 21, new(3));

        var samWeek2 = _service.ScoreWeek(sam, Pick(2, ["KC", "SF", "LAR"], ("sd2a", "DOG"), ("sd2b", "DOG2")),
            [Game("kc", 2, "KC", 3, true), Game("sf", 2, "SF", 3, true), Game("lar", 2, "LAR", 2, true), DogGame("sd2a", 2, 7, true), DogGame("sd2b", 2, 9, true, "DOG2")]);
        // The source fixture's winsByTier accidentally omits LAR's Yellow win; its own
        // point value is 2. Assert the authoritative tier rule rather than losing data.
        AssertGolden(samWeek2, 8, 3, 16, 11, 27, true, true, false, 27, new(0, 2, 1));
        var samSeason = _service.ScoreSeason([samWeek1, samWeek2], []);
        samSeason.Pools.Should().Be(new PoolTotals(42, 16, 26, 0));
        samSeason.GoingPerfRecordTotal.Should().Be(27);
    }

    [Fact]
    public void ScoreWeek_UsesOfficialLineAndRetainsWinningDogMargin()
    {
        var roster = Roster(("A", 4), ("B", 3), ("C", 2), ("D", 1), ("E", 1), ("F", 1), ("G", 1), ("H", 1), ("I", 1));
        var score = _service.ScoreWeek(roster, Pick(1, ["A", "B", "C"], ("dog", "DOG")),
            [Game("a", 1, "A", 4, true), Game("b", 1, "B", 3, true), Game("c", 1, "C", 2, true), new("dog", 1, "FAV", "DOG", 6.1m, 6.5m, new(10, 17))]);

        score.DogPoints.Should().Be(7); // official +6.5, never the API's +6.1
        score.HasUnconfirmedDogPoints.Should().BeFalse();
        score.WinningDogMargins.Should().ContainSingle().Which.Should().Be(7);
    }

    [Fact]
    public void ScoreWeek_FallbackApiLineIsMarkedUnconfirmed_AndTiesAreLosses()
    {
        var roster = Roster(("A", 4), ("B", 3), ("C", 2), ("D", 1), ("E", 1), ("F", 1), ("G", 1), ("H", 1), ("I", 1));
        var score = _service.ScoreWeek(roster, Pick(1, ["A", "B", "C"], ("dog", "DOG")),
            [Game("a", 1, "A", 4, false), Game("b", 1, "B", 3, false), Game("c", 1, "C", 2, false), new("dog", 1, "FAV", "DOG", 6.5m, null, new(14, 14))]);

        score.HasUnconfirmedDogPoints.Should().BeFalse(); // no points were derived from fallback line
        score.IsOfer.Should().BeTrue();
        score.WeekPotential.Should().Be(19); // 4 + 3 + 2 + ceil(6.5) + 3
    }

    [Fact]
    public void ScoreWeek_RejectsByesAndInvalidDogAllowance()
    {
        var roster = Roster(("A", 4, (int?)1), ("B", 3, null), ("C", 2, null), ("D", 1, null), ("E", 1, null), ("F", 1, null), ("G", 1, null), ("H", 1, null), ("I", 1, null));
        var act = () => _service.ScoreWeek(roster, Pick(1, ["A", "B", "C"], ("dog", "DOG")), [Game("a", 1, "A", 4, true), Game("b", 1, "B", 3, true), Game("c", 1, "C", 2, true), DogGame("dog", 1, 6, true)]);
        act.Should().Throw<ArgumentException>().WithMessage("*bye*");
    }

    [Fact]
    public void ScoreSeason_KeepsPoolsSeparate_AndUsesMaxMinQualifyingPotential()
    {
        var goingPerf = new WeeklyScore(8, 3, 9, new(20, 9, 11, 0), new(1), true, true, false, 20, true, false, []);
        var ofer = new WeeklyScore(0, 0, 0, new(0, 0, 0, 0), new(), false, false, true, 14, true, false, []);
        var season = _service.ScoreSeason([goingPerf, ofer], [19]);
        season.Pools.Should().Be(new PoolTotals(39, 9, 11, 19));
        season.GoingPerfRecordTotal.Should().Be(20);
        season.OferRecordTotal.Should().Be(14);
    }

    [Fact]
    public void ScorePlayoffPick_AppliesMultiplierAndBonusesOnlyToPlayoffResult()
    {
        var roster = Roster(("A", 4), ("B", 3), ("C", 2), ("D", 1), ("E", 1), ("F", 1), ("G", 1), ("H", 1), ("I", 1));
        _service.ScorePlayoffPick(roster, new("A", PlayoffRound.Divisional, true, true, true)).Should().Be(29); // 4x4 + 3 + 10
    }

    private static void AssertGolden(WeeklyScore score, decimal starters, decimal bonus, decimal dog, decimal regular, decimal main, bool perfect, bool perf, bool ofer, decimal potential, TierWinCounts tiers)
    {
        score.StarterPoints.Should().Be(starters); score.PerfectWeekBonus.Should().Be(bonus); score.DogPoints.Should().Be(dog);
        score.Pools.RegularSeason.Should().Be(regular); score.Pools.Main.Should().Be(main); score.Pools.Playoff.Should().Be(0);
        score.IsPerfectWeek.Should().Be(perfect); score.IsGoingPerf.Should().Be(perf); score.IsOfer.Should().Be(ofer); score.WeekPotential.Should().Be(potential); score.WinsByTier.Should().Be(tiers);
    }

    private static SeasonRoster Roster(params (string Id, int Value, int? Bye)[] teams) => new(2026, teams.Select(t => new Team(t.Id, t.Id, 5 - t.Value, t.Bye ?? 18)));
    private static SeasonRoster Roster(params (string Id, int Value)[] teams) => Roster(teams.Select(t => (t.Id, t.Value, (int?)null)).ToArray());
    private static WeeklyPick Pick(int week, string[] starters, params (string GameId, string TeamId)[] dogs) => new(week, starters, dogs.Select(x => new DogPick(x.GameId, x.TeamId)), dogs.Length);
    private static WeeklyGame Game(string id, int week, string team, int points, bool won) => new(id, week, team, $"{team}-opponent", 1, 1, won ? new(1, 0) : new(0, 1));
    private static WeeklyGame DogGame(string id, int week, decimal spread, bool won, string teamId = "DOG") => new(id, week, "FAV", teamId, spread, spread, won ? new(0, 1) : new(1, 0));
}
