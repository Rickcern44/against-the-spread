using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Contracts;

/// <summary>Stable routes for the Phase 4 application API.</summary>
public static class AppApiRoutes
{
    public const string Standings = "/api/standings";
    public const string SeasonRoster = "/api/roster";
    public static string WeekGames(int week) => $"/api/weeks/{week}/games";
    public static string WeekPicks(int week) => $"/api/weeks/{week}/picks";
    public static string WeekRecommendation(int week) => $"/api/weeks/{week}/recommendation";
    public static string WeekData(int week) => $"/api/weeks/{week}/data";
    public static string WeekDetail(int week) => $"/api/weeks/{week}/detail";
}

/// <summary>Every API operation returns this envelope, including expected empty states.</summary>
public sealed record ApiResponse<T>(T? Data, ApiProblem? Problem)
{
    public bool Succeeded => Problem is null;
    public static ApiResponse<T> Success(T data) => new(data, null);
    public static ApiResponse<T> Failure(ApiProblem problem) => new(default, problem);
}

public sealed record ApiProblem(ApiProblemCode Code, string Message, bool IsRetryable = false);

public enum ApiProblemCode { WeekNotPulled, ProviderUnavailable, NoPicksLogged, SeasonNotStarted, RosterNotSet }

/// <summary>GET /api/weeks/{week}/games. Team IDs are canonical ESPN team IDs.</summary>
public sealed record WeekGamesResponse(int Week, IReadOnlyList<WeeklyGame> Games, IReadOnlyList<Team> Teams);

/// <summary>GET /api/weeks/{week}/picks.</summary>
public sealed record WeekPicksResponse(WeeklyPick Pick);

/// <summary>PUT /api/weeks/{week}/picks. The URL week and Pick.Week must agree.</summary>
public sealed record SaveWeekPicksRequest(WeeklyPick Pick);

/// <summary>A ranked recommendation for one team in one game.</summary>
public sealed record RankedRecommendation(
    string TeamId,
    string GameId,
    decimal WinProbability,
    decimal ExpectedValue,
    int Rank);

/// <summary>GET /api/weeks/{week}/recommendation.</summary>
public sealed record WeekRecommendationResponse(
    int Week,
    IReadOnlyList<RankedRecommendation> Starters,
    IReadOnlyList<RankedRecommendation> Dogs);

/// <summary>One regular-season result retained for standings history.</summary>
public sealed record WeekScoreHistory(int Week, WeeklyScore Score);

/// <summary>
/// GET /api/standings. This wraps SeasonScore rather than extending it: SeasonScore remains
/// an aggregate scoring value object while this response owns API-only week-by-week history.
/// </summary>
public sealed record StandingsResponse(SeasonScore Season, IReadOnlyList<WeekScoreHistory> WeeklyHistory);

public enum DataPullKind { Lines, Results }

public sealed record DataPullStatus(bool Succeeded, DateTimeOffset? CompletedAt, ApiProblem? Problem = null);

/// <summary>GET /api/weeks/{week}/data.</summary>
public sealed record WeekDataStatusResponse(int Week, DataPullStatus Lines, DataPullStatus Results);

/// <summary>POST /api/weeks/{week}/data. Exactly one command shape is supplied by callers.</summary>
public sealed record PostWeekDataRequest(DataPullKind? Pull = null, OfficialLineOverrideRequest? OfficialLineOverride = null);

/// <summary>Manual commissioner-line write. Positive points are laid by the favorite.</summary>
public sealed record OfficialLineOverrideRequest(string GameId, decimal OfficialLine);

/// <summary>GET /api/roster. Returns the current season's drafted nine-team roster, or an
/// <see cref="ApiProblemCode.RosterNotSet"/> problem if the user has not drafted one yet.</summary>
public sealed record SeasonRosterResponse(int Season, IReadOnlyList<Team> Teams);

/// <summary>PUT /api/roster. Must supply exactly <see cref="Models.SeasonRoster.RequiredTeamCount"/> unique team ids.</summary>
public sealed record SaveSeasonRosterRequest(IReadOnlyList<string> TeamIds);

/// <summary>
/// Whether this week's saved pick has any known post-deadline correction history, and why not
/// when it doesn't. Storage today only ever keeps the single, latest saved <see cref="WeeklyPick"/>
/// per week - it does not retain prior versions or the timestamp a correction was made - so
/// <see cref="HasTrackedHistory"/> is always <c>false</c> until that persistence gap is closed.
/// This type exists so the history view can say so honestly rather than inventing a history.
/// </summary>
public sealed record WeekCorrectionStatus(bool HasTrackedHistory, string Message)
{
    public static WeekCorrectionStatus NotTracked() => new(false,
        "Correction history isn't persisted yet - only the most recently saved pick is kept, " +
        "so a past edit made after this week's deadline can't be shown here.");
}

/// <summary>GET /api/weeks/{week}/detail. A read-only rollup of a single week for the history
/// view: the slate, the saved pick (if any), its score (if scorable), and correction-history
/// status. Mirrors <see cref="WeekGamesResponse"/> plus <see cref="WeekPicksResponse"/> rather
/// than replacing either, since Picks.razor still owns the live edit flow for the current week.</summary>
public sealed record WeekDetailResponse(
    int Week,
    IReadOnlyList<WeeklyGame> Games,
    IReadOnlyList<Team> Teams,
    WeeklyPick? Pick,
    WeeklyScore? Score,
    WeekCorrectionStatus Correction);
