using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

public sealed class ScoringService : IScoringService
{
    public WeeklyScore ScoreWeek(SeasonRoster roster, WeeklyPick pick, IEnumerable<WeeklyGame> games)
    {
        var slate = games?.Where(g => g.Week == pick.Week).ToDictionary(g => g.Id, StringComparer.OrdinalIgnoreCase)
            ?? throw new ArgumentNullException(nameof(games));
        pick.Validate(roster, slate.Values);
        foreach (var game in slate.Values) game.Validate();

        var starterGames = pick.StarterTeamIds.Select(id => (Team: roster.GetTeam(id), Game: slate.Values.Single(g => Matches(g, id)))).ToList();
        var dogGames = pick.DogPicks.Select(d => (Pick: d, Game: slate[d.GameId])).ToList();
        var complete = starterGames.All(x => x.Game.Result?.IsFinal == true) && dogGames.All(x => x.Game.Result?.IsFinal == true);
        if (!complete)
            return new WeeklyScore(0, 0, 0, new PoolTotals(0, 0, 0, 0), new TierWinCounts(), false, false, false, null, false, false, Array.Empty<int>());

        var winnerIds = starterGames.Select(x => x.Game.Result!.WinnerTeamId(x.Game.FavoriteTeamId, x.Game.UnderdogTeamId)).ToList();
        var starterWins = starterGames.Zip(winnerIds).Where(x => string.Equals(x.First.Team.Id, x.Second, StringComparison.OrdinalIgnoreCase)).Select(x => x.First.Team).ToList();
        var starterPoints = starterWins.Sum(t => (decimal)t.PointValue);
        var perfect = starterWins.Count == WeeklyPick.RequiredStarterCount;
        var bonus = perfect ? 3 : 0;
        var winningDogs = dogGames.Where(x => string.Equals(x.Game.Result!.WinnerTeamId(x.Game.FavoriteTeamId, x.Game.UnderdogTeamId), x.Pick.TeamId, StringComparison.OrdinalIgnoreCase)).ToList();
        var dogPoints = winningDogs.Sum(x => decimal.Ceiling(x.Game.DogSpread(x.Pick.TeamId)));
        var dogWonAll = winningDogs.Count == dogGames.Count;
        var starterLostAll = starterWins.Count == 0;
        var dogLostAll = winningDogs.Count == 0;
        var goingPerf = perfect && dogWonAll;
        var ofer = starterLostAll && dogLostAll;
        var potential = starterGames.Sum(x => (decimal)x.Team.PointValue) + dogGames.Sum(x => decimal.Ceiling(x.Game.DogSpread(x.Pick.TeamId))) + 3;
        var tiers = starterWins.Aggregate(new TierWinCounts(), (counts, team) => counts.Add(team.Tier));
        var regular = starterPoints + bonus;
        var unconfirmed = winningDogs.Any(x => !x.Game.IsOfficialLineConfirmed);
        var margins = winningDogs.Select(x => x.Game.Result!.MarginOfVictory).ToList();
        return new WeeklyScore(starterPoints, bonus, dogPoints, new PoolTotals(regular + dogPoints, dogPoints, regular, 0), tiers, perfect, goingPerf, ofer, potential, true, unconfirmed, margins);
    }

    public decimal ScorePlayoffPick(SeasonRoster roster, PlayoffPick pick)
    {
        var team = roster.GetTeam(pick.TeamId);
        return (pick.Won ? team.PointValue * pick.RoundMultiplier : 0) + (pick.QualifiedForPlayoffs ? 3 : 0) + (pick.EarnedConferenceWinnerBye ? 10 : 0);
    }

    public SeasonScore ScoreSeason(IEnumerable<WeeklyScore> weeklyScores, IEnumerable<decimal> playoffScores)
    {
        var weeks = weeklyScores?.ToList() ?? throw new ArgumentNullException(nameof(weeklyScores));
        var playoff = playoffScores?.Sum() ?? throw new ArgumentNullException(nameof(playoffScores));
        var regular = weeks.Sum(w => w.Pools.RegularSeason);
        var dog = weeks.Sum(w => w.Pools.Dog);
        var tiers = weeks.Aggregate(new TierWinCounts(), (total, week) => total + week.WinsByTier);
        return new SeasonScore(new PoolTotals(regular + dog + playoff, dog, regular, playoff), tiers,
            weeks.Where(w => w.IsGoingPerf).Select(w => w.WeekPotential).Max(),
            weeks.Where(w => w.IsOfer).Select(w => w.WeekPotential).Min(),
            weeks.SelectMany(w => w.WinningDogMargins).OrderDescending().ToList());
    }

    private static bool Matches(WeeklyGame game, string teamId) => string.Equals(game.FavoriteTeamId, teamId, StringComparison.OrdinalIgnoreCase) || string.Equals(game.UnderdogTeamId, teamId, StringComparison.OrdinalIgnoreCase);
}
