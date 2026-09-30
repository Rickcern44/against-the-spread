using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>API endpoint backing the Phase 4 app's <c>GET /api/weeks/{week}/games</c> contract.</summary>
public sealed class WeekGamesFunction
{
    private readonly ILogger<WeekGamesFunction> _logger;
    private readonly IStorageService _storageService;

    public WeekGamesFunction(ILogger<WeekGamesFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    private static int CurrentSeason() =>
        int.TryParse(Environment.GetEnvironmentVariable("NFL_SEASON"), out var season) ? season : DateTime.UtcNow.Year;

    private static async Task WriteJsonAsync<T>(HttpResponseData response, T value, CancellationToken cancellationToken)
    {
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value), cancellationToken);
    }

    /// <summary>
    /// GET /api/weeks/{week}/games
    /// Returns the stored slate for the week plus the roster teams referenced by it, or an
    /// <see cref="ApiProblemCode.WeekNotPulled"/> problem if no games are stored yet.
    /// </summary>
    [Function("GetWeekGames")]
    public async Task<HttpResponseData> GetWeekGames(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks/{week:int}/games")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        try
        {
            var season = CurrentSeason();
            var games = await _storageService.GetWeeklyGamesAsync(season, week, cancellationToken);
            if (games is null)
            {
                var notPulled = req.CreateResponse(HttpStatusCode.NotFound);
                await WriteJsonAsync(notPulled, new ApiProblem(ApiProblemCode.WeekNotPulled, $"Week {week} has not been pulled."), cancellationToken);
                return notPulled;
            }

            var roster = await _storageService.GetRosterAsync(season, cancellationToken);
            var referencedIds = games.SelectMany(g => new[] { g.FavoriteTeamId, g.UnderdogTeamId }).ToHashSet(StringComparer.OrdinalIgnoreCase);
            IReadOnlyList<Team> teams = roster is null
                ? Array.Empty<Team>()
                : roster.Teams.Where(t => referencedIds.Contains(t.Id)).ToList();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new WeekGamesResponse(week, games, teams), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week {Week} games", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve week games" }, cancellationToken);
            return errorResponse;
        }
    }
}
