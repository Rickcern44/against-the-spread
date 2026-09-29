using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Fixtures;

/// <summary>Deterministic full-week fixture for Web development and bUnit tests; never makes HTTP calls.</summary>
public sealed class FakeApiClient : IAppApiClient
{
    private const int CurrentSeason = 2026;

    // Deliberately starts unset so the first-run onboarding gate has something to gate on
    // in dev/tests; a real deployment starts the same way until a roster is saved.
    private SeasonRosterResponse? roster;

    private static readonly IReadOnlyList<WeeklyGame> WeekOneGames = new[]
    {
        new WeeklyGame("2026-01-DAL-PHI", 1, "21", "6", 6.5m, 7m, new GameResult(27, 20)),
        new WeeklyGame("2026-01-KC-LAC", 1, "12", "24", 3.5m, null, new GameResult(24, 21)),
        new WeeklyGame("2026-01-BUF-BAL", 1, "2", "33", 2.5m, 2.5m),
        new WeeklyGame("2026-01-DET-GB", 1, "8", "9", 1.5m, 1m)
    };

    private static readonly IReadOnlyList<Team> WeekOneTeams = new[]
    {
        new Team("21", "Philadelphia Eagles", 1, 9), new Team("6", "Dallas Cowboys", 2, 10),
        new Team("12", "Kansas City Chiefs", 1, 10), new Team("24", "Los Angeles Chargers", 2, 5),
        new Team("2", "Buffalo Bills", 2, 7), new Team("33", "Baltimore Ravens", 2, 13),
        new Team("8", "Detroit Lions", 2, 8), new Team("9", "Green Bay Packers", 3, 5)
    };

    private static readonly WeeklyPick WeekOnePick = new(1, new[] { "21", "12", "2" }, new[] { new DogPick("2026-01-DAL-PHI", "6") });
    private static readonly WeekRecommendationResponse WeekOneRecommendation = new(1,
        new[] { new RankedRecommendation("21", "2026-01-DAL-PHI", .70m, 2.80m, 1), new RankedRecommendation("12", "2026-01-KC-LAC", .60m, 1.80m, 2), new RankedRecommendation("2", "2026-01-BUF-BAL", .57m, 1.14m, 3) },
        new[] { new RankedRecommendation("6", "2026-01-DAL-PHI", .30m, 2.10m, 1), new RankedRecommendation("24", "2026-01-KC-LAC", .40m, 1.40m, 2), new RankedRecommendation("33", "2026-01-BUF-BAL", .43m, 1.08m, 3) });

    private static readonly WeekDataStatusResponse WeekOneData = new(1,
        new DataPullStatus(true, new DateTimeOffset(2026, 9, 8, 14, 0, 0, TimeSpan.Zero)),
        new DataPullStatus(true, new DateTimeOffset(2026, 9, 15, 4, 0, 0, TimeSpan.Zero)));

    private static readonly StandingsResponse Standings = new(
        new SeasonScore(new PoolTotals(11m, 2m, 13m, 0m), new TierWinCounts(Green: 2, Blue: 1), 1m, 0m, new[] { 7 }),
        new[] { new WeekScoreHistory(1, new WeeklyScore(11m, 0m, 2m, new PoolTotals(11m, 2m, 13m, 0m), new TierWinCounts(Green: 2, Blue: 1), false, true, false, 13m, true, false, new[] { 7 })) });

    public Task<ApiResponse<WeekGamesResponse>> GetWeekGamesAsync(int week, CancellationToken cancellationToken = default) =>
        Task.FromResult(week == 1 ? ApiResponse<WeekGamesResponse>.Success(new(1, WeekOneGames, WeekOneTeams)) : NotPulled<WeekGamesResponse>(week));
    public Task<ApiResponse<WeekPicksResponse>> GetWeekPicksAsync(int week, CancellationToken cancellationToken = default) =>
        Task.FromResult(week == 1 ? ApiResponse<WeekPicksResponse>.Success(new(WeekOnePick)) : ApiResponse<WeekPicksResponse>.Failure(new(ApiProblemCode.NoPicksLogged, $"No picks have been logged for week {week}.")));
    public Task<ApiResponse<WeekPicksResponse>> SaveWeekPicksAsync(int week, SaveWeekPicksRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(ApiResponse<WeekPicksResponse>.Success(new(request.Pick)));
    public Task<ApiResponse<WeekRecommendationResponse>> GetWeekRecommendationAsync(int week, CancellationToken cancellationToken = default) =>
        Task.FromResult(week == 1 ? ApiResponse<WeekRecommendationResponse>.Success(WeekOneRecommendation) : NotPulled<WeekRecommendationResponse>(week));
    public Task<ApiResponse<StandingsResponse>> GetStandingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ApiResponse<StandingsResponse>.Success(Standings));
    public Task<ApiResponse<WeekDataStatusResponse>> GetWeekDataAsync(int week, CancellationToken cancellationToken = default) =>
        Task.FromResult(week == 1 ? ApiResponse<WeekDataStatusResponse>.Success(WeekOneData) : week == 17
            ? ApiResponse<WeekDataStatusResponse>.Success(new(17,
                new DataPullStatus(false, null, new(ApiProblemCode.ProviderUnavailable, "Odds provider is temporarily unavailable.", true)),
                new DataPullStatus(false, null, new(ApiProblemCode.ProviderUnavailable, "Scores provider is temporarily unavailable.", true))))
            : NotPulled<WeekDataStatusResponse>(week));
    public Task<ApiResponse<WeekDataStatusResponse>> PostWeekDataAsync(int week, PostWeekDataRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(week == 1 ? ApiResponse<WeekDataStatusResponse>.Success(WeekOneData) : NotPulled<WeekDataStatusResponse>(week));

    public Task<ApiResponse<SeasonRosterResponse>> GetSeasonRosterAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(roster is not null
            ? ApiResponse<SeasonRosterResponse>.Success(roster)
            : ApiResponse<SeasonRosterResponse>.Failure(new(ApiProblemCode.RosterNotSet, "No season roster has been drafted yet.")));

    public Task<ApiResponse<SeasonRosterResponse>> SaveSeasonRosterAsync(SaveSeasonRosterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TeamIds is null || request.TeamIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != SeasonRoster.RequiredTeamCount)
        {
            throw new ArgumentException($"A season roster must contain exactly {SeasonRoster.RequiredTeamCount} unique teams.", nameof(request));
        }

        var teams = request.TeamIds
            .Select(id => NflTeamDirectory.AllTeams.Single(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        roster = new SeasonRosterResponse(CurrentSeason, teams);
        return Task.FromResult(ApiResponse<SeasonRosterResponse>.Success(roster));
    }

    private static ApiResponse<T> NotPulled<T>(int week) => ApiResponse<T>.Failure(new(ApiProblemCode.WeekNotPulled, $"Week {week} has not been pulled."));
}
