using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

public sealed class StarterRecommendationService : IStarterRecommendationService
{
    private const decimal TiebreakThreshold = 0.05m;

    public StarterRecommendation Recommend(SeasonRoster roster, IEnumerable<WeeklyGame> games, int week)
    {
        ArgumentNullException.ThrowIfNull(roster);
        var slate = games?.Where(g => g.Week == week).ToList() ?? throw new ArgumentNullException(nameof(games));

        var byeTeams = roster.Teams.Where(t => t.ByeWeek == week).Select(t => t.Id).ToList();
        var benchedByCollision = BenchedCollisionLosers(roster, slate);

        var ranked = roster.Teams
            .Where(t => t.ByeWeek != week)
            .Where(t => !benchedByCollision.Contains(t.Id))
            .Select(t => BuildCandidate(t, slate))
            .Where(c => c is not null)
            .Select(c => c!)
            .OrderByDescending(c => c.ExpectedValue)
            .ToList();

        var tiebreak = EvaluateTiebreak(ranked);

        var recommended = ranked.Count <= 3
            ? ranked.Select(c => c.TeamId).ToList()
            : new List<string>
            {
                ranked[0].TeamId,
                ranked[1].TeamId,
                tiebreak.Triggered ? tiebreak.WinnerTeamId! : ranked[2].TeamId
            };

        return new StarterRecommendation(week, byeTeams, ranked, recommended, tiebreak);
    }

    private static StarterCandidate? BuildCandidate(Team team, List<WeeklyGame> slate)
    {
        var game = slate.FirstOrDefault(g => Matches(g, team.Id));
        if (game is null) return null;

        var isFavorite = string.Equals(game.FavoriteTeamId, team.Id, StringComparison.OrdinalIgnoreCase);
        var line = game.ScoringLine ?? throw new InvalidOperationException($"Game {game.Id} has no line to recommend from.");
        var margin = isFavorite ? line : -line;

        var rawProbability = SpreadWinProbability.RawProbability(margin);
        var winProbability = SpreadWinProbability.Round(rawProbability);
        // EV is computed from the unrounded probability, then rounded once at the end (matches
        // the pinned starter fixture); the dog engine rounds probability first instead - see
        // DogRecommendationService for why the two pipelines diverge.
        var expectedValue = Math.Round((decimal)rawProbability * team.PointValue, 4, MidpointRounding.AwayFromZero);

        return new StarterCandidate(team.Id, game.Id, team.PointValue, isFavorite, margin, winProbability, expectedValue);
    }

    /// <summary>When two rostered teams play each other, one is guaranteed to lose. Bench the
    /// underdog side so it never displaces an independent game from the ranking - but never bench
    /// both, so the favored side stays eligible.</summary>
    private static HashSet<string> BenchedCollisionLosers(SeasonRoster roster, List<WeeklyGame> slate)
    {
        var benched = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var game in slate)
        {
            if (roster.Contains(game.FavoriteTeamId) && roster.Contains(game.UnderdogTeamId))
                benched.Add(game.UnderdogTeamId);
        }
        return benched;
    }

    private static bool Matches(WeeklyGame game, string teamId) =>
        string.Equals(game.FavoriteTeamId, teamId, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(game.UnderdogTeamId, teamId, StringComparison.OrdinalIgnoreCase);

    private static StarterTiebreak EvaluateTiebreak(List<StarterCandidate> ranked)
    {
        if (ranked.Count < 4)
            return new StarterTiebreak(false, Array.Empty<string>(), 0m, new Dictionary<string, decimal>(), null);

        var third = ranked[2];
        var fourth = ranked[3];
        var gap = Math.Abs(third.ExpectedValue - fourth.ExpectedValue);
        if (gap > TiebreakThreshold)
            return new StarterTiebreak(false, Array.Empty<string>(), 0m, new Dictionary<string, decimal>(), null);

        var locked = ranked[0].WinProbability * ranked[1].WinProbability;
        var thirdProduct = Math.Round(locked * third.WinProbability, 4, MidpointRounding.AwayFromZero);
        var fourthProduct = Math.Round(locked * fourth.WinProbability, 4, MidpointRounding.AwayFromZero);
        var winner = thirdProduct >= fourthProduct ? third.TeamId : fourth.TeamId;

        return new StarterTiebreak(
            true,
            new[] { third.TeamId, fourth.TeamId },
            gap,
            new Dictionary<string, decimal> { [third.TeamId] = thirdProduct, [fourth.TeamId] = fourthProduct },
            winner);
    }
}
