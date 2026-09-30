namespace AgainstTheSpread.Core.Models;

/// <summary>One regular-season game. Lines are positive points laid by <see cref="FavoriteTeamId"/>.</summary>
public sealed record WeeklyGame(
    string Id,
    int Week,
    string FavoriteTeamId,
    string UnderdogTeamId,
    decimal? ApiLine,
    decimal? OfficialLine,
    GameResult? Result = null,
    DateTimeOffset? Kickoff = null)
{
    public decimal? ScoringLine => OfficialLine ?? ApiLine;
    public bool IsOfficialLineConfirmed => OfficialLine.HasValue;

    /// <summary>True once this specific game has kicked off, per the league's dog-pick deadline
    /// (each dog closes at its own kickoff). Unknown kickoff times are treated as not yet locked.</summary>
    public bool HasKickedOff(DateTimeOffset now) => Kickoff.HasValue && now >= Kickoff.Value;

    public decimal DogSpread(string teamId)
    {
        if (!string.Equals(teamId, UnderdogTeamId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only the underdog has a dog spread.", nameof(teamId));
        return Math.Abs(ScoringLine ?? throw new InvalidOperationException("A line is required to score a dog."));
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(FavoriteTeamId) || string.IsNullOrWhiteSpace(UnderdogTeamId))
            throw new ArgumentException("Game and team ids are required.");
        if (Week is < 1 or > 18) throw new ArgumentOutOfRangeException(nameof(Week));
        if (string.Equals(FavoriteTeamId, UnderdogTeamId, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("A game needs two teams.");
        if (ApiLine is < 0 || OfficialLine is < 0) throw new ArgumentOutOfRangeException("Lines must be positive points laid by the favorite.");
        Result?.Validate(FavoriteTeamId, UnderdogTeamId);
    }
}

/// <summary>Final scores. A tied game is final but has no winner.</summary>
public sealed record GameResult(int FavoriteScore, int UnderdogScore, bool IsFinal = true)
{
    public string? WinnerTeamId(string favoriteTeamId, string underdogTeamId) => !IsFinal || FavoriteScore == UnderdogScore
        ? null
        : FavoriteScore > UnderdogScore ? favoriteTeamId : underdogTeamId;

    public int MarginOfVictory => Math.Abs(FavoriteScore - UnderdogScore);

    public void Validate(string favoriteTeamId, string underdogTeamId)
    {
        if (FavoriteScore < 0 || UnderdogScore < 0) throw new ArgumentOutOfRangeException("Scores cannot be negative.");
    }
}
