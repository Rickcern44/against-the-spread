using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>API endpoint backing the Phase 4 app's <c>GET /api/weeks/{week}/detail</c> contract,
/// which powers the history/week-detail view (browsing a past week's slate, saved pick, and
/// score in one call rather than three separate ones).</summary>
public sealed class WeekDetailFunction
{
    private readonly ILogger<WeekDetailFunction> _logger;
    private readonly IStorageService _storageService;
    private readonly IScoringService _scoringService;

    public WeekDetailFunction(ILogger<WeekDetailFunction> logger, IStorageService storageService, IScoringService scoringService)
    {
        _logger = logger;
        _storageService = storageService;
        _scoringService = scoringService;
    }

    private static int CurrentSeason() =>
        int.TryParse(Environment.GetEnvironmentVariable("NFL_SEASON"), out var season) ? season : DateTime.UtcNow.Year;

    private static async Task WriteJsonAsync<T>(HttpResponseData response, T value, CancellationToken cancellationToken)
    {
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value), cancellationToken);
    }

    /// <summary>
    /// GET /api/weeks/{week}/detail
    /// Returns the stored slate, the saved pick (if any), its score (if it still validates), and
    /// an honest correction-history status. Returns an <see cref="ApiProblemCode.WeekNotPulled"/>
    /// problem if no slate is stored for the week yet - the same condition <see cref="WeekGamesFunction"/>
    /// reports, since this endpoint is a superset of that one for the history view.
    /// </summary>
    [Function("GetWeekDetail")]
    public async Task<HttpResponseData> GetWeekDetail(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks/{week:int}/detail")] HttpRequestData req,
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

            var pick = await _storageService.GetWeeklyPickAsync(season, week, cancellationToken);
            WeeklyScore? score = null;
            if (pick is not null && roster is not null)
            {
                try
                {
                    score = _scoringService.ScoreWeek(roster, pick, games);
                }
                catch (ArgumentException ex)
                {
                    // Same gap StandingsFunction tolerates: a stored pick that no longer
                    // validates against the current slate (e.g. an official-line correction)
                    // shouldn't crash the history view - it just shows no score for that week.
                    _logger.LogWarning(ex, "Week {Week} detail: saved pick no longer validates, omitting score", week);
                }
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new WeekDetailResponse(week, games, teams, pick, score, WeekCorrectionStatus.NotTracked()), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week {Week} detail", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve week detail" }, cancellationToken);
            return errorResponse;
        }
    }
}
