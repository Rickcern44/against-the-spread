namespace AgainstTheSpread.Core.Models;

/// <summary>One roster team's ranking data for a week's starter recommendation. EV is a ranking
/// number only, never a projected score.</summary>
public sealed record StarterCandidate(
    string TeamId,
    string GameId,
    int PointValue,
    bool IsFavorite,
    decimal ExpectedMargin,
    decimal WinProbability,
    decimal ExpectedValue);

/// <summary>Whether the perfect-week tiebreak fired for the contested third starting slot, and why.</summary>
public sealed record StarterTiebreak(
    bool Triggered,
    IReadOnlyList<string> CandidateTeamIds,
    decimal EvGap,
    IReadOnlyDictionary<string, decimal> PerfectWeekProbability,
    string? WinnerTeamId);

/// <summary>The full starter recommendation for one week: every eligible roster team ranked by
/// EV, the top 3, bye teams excluded from ranking, and the tiebreak reasoning if it fired.</summary>
public sealed record StarterRecommendation(
    int Week,
    IReadOnlyList<string> ByeTeams,
    IReadOnlyList<StarterCandidate> Ranking,
    IReadOnlyList<string> RecommendedStarters,
    StarterTiebreak Tiebreak);
