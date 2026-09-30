namespace AgainstTheSpread.Core.Models;

/// <summary>One underdog on the slate, ranked by EV. Not limited to rostered teams — every game's
/// underdog is a row (docs/swan-league-spec.md §2.1, §7.5).</summary>
public sealed record DogCandidate(
    string TeamId,
    string GameId,
    string OpponentTeamId,
    decimal DogSpread,
    int DogPoints,
    decimal WinProbability,
    decimal ExpectedValue);

/// <summary>The full ranked dog list for one week and the top `dogAllowance` picks it implies.</summary>
public sealed record DogRecommendation(
    int Week,
    IReadOnlyList<DogCandidate> Ranking,
    IReadOnlyList<string> RecommendedDogs);
