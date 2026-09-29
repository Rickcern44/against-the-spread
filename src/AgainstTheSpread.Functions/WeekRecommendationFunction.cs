using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>API endpoint backing the Phase 4 app's <c>GET /api/weeks/{week}/recommendation</c> contract.</summary>
public sealed class WeekRecommendationFunction
{
    private readonly ILogger<WeekRecommendationFunction> _logger;
    private readonly IStorageService _storageService;
    private readonly IStarterRecommendationService _starterService;
    private readonly IDogRecommendationService _dogService;

    public WeekRecommendationFunction(
        ILogger<WeekRecommendationFunction> logger,
        IStorageService storageService,
        IStarterRecommendationService starterService,
        IDogRecommendationService dogService)
    {
        _logger = logger;
        _storageService = storageService;
        _starterService = starterService;
        _dogService = dogService;
    }

    private static int CurrentSeason() =>
        int.TryParse(Environment.GetEnvironmentVariable("NFL_SEASON"), out var season) ? season : DateTime.UtcNow.Year;

    private static async Task WriteJsonAsync<T>(HttpResponseData response, T value, CancellationToken cancellationToken)
    {
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value), cancellationToken);
    }

    /// <summary>
    /// GET /api/weeks/{week}/recommendation
    /// Ranks starters and dogs for the week using the current season's roster and stored games.
    /// </summary>
    [Function("GetWeekRecommendation")]
    public async Task<HttpResponseData> GetWeekRecommendation(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks/{week:int}/recommendation")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        try
        {
            var season = CurrentSeason();
            var roster = await _storageService.GetRosterAsync(season, cancellationToken);
            if (roster is null)
            {
                var notSet = req.CreateResponse(HttpStatusCode.NotFound);
                await WriteJsonAsync(notSet, new ApiProblem(ApiProblemCode.RosterNotSet, "No season roster has been drafted yet."), cancellationToken);
                return notSet;
            }

            var games = await _storageService.GetWeeklyGamesAsync(season, week, cancellationToken);
            if (games is null)
            {
                var notPulled = req.CreateResponse(HttpStatusCode.NotFound);
                await WriteJsonAsync(notPulled, new ApiProblem(ApiProblemCode.WeekNotPulled, $"Week {week} has not been pulled."), cancellationToken);
                return notPulled;
            }

            var starterRecommendation = _starterService.Recommend(roster, games, week);
            var dogRecommendation = _dogService.Recommend(games, week);

            var starters = starterRecommendation.Ranking
                .Select((candidate, index) => new RankedRecommendation(candidate.TeamId, candidate.GameId, candidate.WinProbability, candidate.ExpectedValue, index + 1))
                .ToList();
            var dogs = dogRecommendation.Ranking
                .Select((candidate, index) => new RankedRecommendation(candidate.TeamId, candidate.GameId, candidate.WinProbability, candidate.ExpectedValue, index + 1))
                .ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new WeekRecommendationResponse(week, starters, dogs), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week {Week} recommendation", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve week recommendation" }, cancellationToken);
            return errorResponse;
        }
    }
}
