using AgainstTheSpread.Core.Contracts;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>The Blazor application's sole seam for the Phase 4 API.</summary>
public interface IAppApiClient
{
    Task<ApiResponse<WeekGamesResponse>> GetWeekGamesAsync(int week, CancellationToken cancellationToken = default);
    Task<ApiResponse<WeekPicksResponse>> GetWeekPicksAsync(int week, CancellationToken cancellationToken = default);
    Task<ApiResponse<WeekPicksResponse>> SaveWeekPicksAsync(int week, SaveWeekPicksRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<WeekRecommendationResponse>> GetWeekRecommendationAsync(int week, CancellationToken cancellationToken = default);
    Task<ApiResponse<StandingsResponse>> GetStandingsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<WeekDataStatusResponse>> GetWeekDataAsync(int week, CancellationToken cancellationToken = default);
    Task<ApiResponse<WeekDataStatusResponse>> PostWeekDataAsync(int week, PostWeekDataRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<SeasonRosterResponse>> GetSeasonRosterAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<SeasonRosterResponse>> SaveSeasonRosterAsync(SaveSeasonRosterRequest request, CancellationToken cancellationToken = default);
}
