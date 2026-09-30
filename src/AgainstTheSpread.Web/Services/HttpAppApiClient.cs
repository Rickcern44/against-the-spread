using System.Net.Http.Json;
using System.Text.Json;
using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;

namespace AgainstTheSpread.Web.Services;

/// <summary>Real HTTP implementation of the Phase 4 app API, calling the Functions endpoints
/// exposed under <see cref="AppApiRoutes"/>. Non-success HTTP responses are mapped to
/// <see cref="ApiResponse{T}.Failure"/> rather than thrown, matching the envelope convention:
/// expected states (e.g. a week that hasn't been pulled yet) are ordinary responses, not errors.</summary>
public sealed class HttpAppApiClient : IAppApiClient
{
    // WeeklyPickJsonConverter is required: WeekPicksResponse wraps WeeklyPick, whose
    // constructor/property type mismatch otherwise breaks System.Text.Json's default
    // parameterized-constructor deserialization (see SeasonModelJsonConverters.cs).
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true, Converters = { new WeeklyPickJsonConverter() } };

    private readonly HttpClient _http;

    public HttpAppApiClient(HttpClient http) => _http = http;

    public Task<ApiResponse<WeekGamesResponse>> GetWeekGamesAsync(int week, CancellationToken cancellationToken = default) =>
        GetAsync<WeekGamesResponse>(AppApiRoutes.WeekGames(week), cancellationToken);

    public Task<ApiResponse<WeekPicksResponse>> GetWeekPicksAsync(int week, CancellationToken cancellationToken = default) =>
        GetAsync<WeekPicksResponse>(AppApiRoutes.WeekPicks(week), cancellationToken);

    public async Task<ApiResponse<WeekPicksResponse>> SaveWeekPicksAsync(int week, SaveWeekPicksRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync(AppApiRoutes.WeekPicks(week), request, cancellationToken);
        return await ToApiResponseAsync<WeekPicksResponse>(response, cancellationToken);
    }

    public Task<ApiResponse<WeekRecommendationResponse>> GetWeekRecommendationAsync(int week, CancellationToken cancellationToken = default) =>
        GetAsync<WeekRecommendationResponse>(AppApiRoutes.WeekRecommendation(week), cancellationToken);

    public Task<ApiResponse<StandingsResponse>> GetStandingsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<StandingsResponse>(AppApiRoutes.Standings, cancellationToken);

    public Task<ApiResponse<WeekDataStatusResponse>> GetWeekDataAsync(int week, CancellationToken cancellationToken = default) =>
        GetAsync<WeekDataStatusResponse>(AppApiRoutes.WeekData(week), cancellationToken);

    public async Task<ApiResponse<WeekDataStatusResponse>> PostWeekDataAsync(int week, PostWeekDataRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(AppApiRoutes.WeekData(week), request, cancellationToken);
        return await ToApiResponseAsync<WeekDataStatusResponse>(response, cancellationToken);
    }

    public Task<ApiResponse<WeekDetailResponse>> GetWeekDetailAsync(int week, CancellationToken cancellationToken = default) =>
        GetAsync<WeekDetailResponse>(AppApiRoutes.WeekDetail(week), cancellationToken);

    public Task<ApiResponse<SeasonRosterResponse>> GetSeasonRosterAsync(CancellationToken cancellationToken = default) =>
        GetAsync<SeasonRosterResponse>(AppApiRoutes.SeasonRoster, cancellationToken);

    public async Task<ApiResponse<SeasonRosterResponse>> SaveSeasonRosterAsync(SaveSeasonRosterRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync(AppApiRoutes.SeasonRoster, new { teamIds = request.TeamIds }, cancellationToken);
        return await ToApiResponseAsync<SeasonRosterResponse>(response, cancellationToken);
    }

    private async Task<ApiResponse<T>> GetAsync<T>(string route, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(route, cancellationToken);
        return await ToApiResponseAsync<T>(response, cancellationToken);
    }

    private static async Task<ApiResponse<T>> ToApiResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(ReadOptions, cancellationToken);
            return data is not null
                ? ApiResponse<T>.Success(data)
                : ApiResponse<T>.Failure(new ApiProblem(ApiProblemCode.ProviderUnavailable, "The server returned an empty response.", true));
        }

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResponse<T>.Failure(problem);
    }

    private static async Task<ApiProblem> TryReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var problem = JsonSerializer.Deserialize<ApiProblem>(body, ReadOptions);
            if (problem is not null) return problem;
        }
        catch (JsonException)
        {
            // The body wasn't a well-formed ApiProblem (e.g. an unhandled 500's ad-hoc error
            // shape) - fall through to a generic, retryable problem below.
        }

        return new ApiProblem(ApiProblemCode.ProviderUnavailable, $"Request failed with status {(int)response.StatusCode}.", true);
    }
}
