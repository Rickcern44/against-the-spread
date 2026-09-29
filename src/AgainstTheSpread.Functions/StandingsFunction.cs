using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>API endpoint backing the Phase 4 app's <c>GET /api/standings</c> contract.</summary>
public sealed class StandingsFunction
{
    private const int MaxRegularSeasonWeek = 18;

    private readonly ILogger<StandingsFunction> _logger;
    private readonly IStorageService _storageService;
    private readonly IScoringService _scoringService;

    public StandingsFunction(ILogger<StandingsFunction> logger, IStorageService storageService, IScoringService scoringService)
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
    /// GET /api/standings
    /// Scores every week that has both a stored slate and a saved pick; weeks with no saved pick
    /// yet are skipped rather than scored as zero.
    /// </summary>
    [Function("GetStandings")]
    public async Task<HttpResponseData> GetStandings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "standings")] HttpRequestData req,
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

            var history = new List<WeekScoreHistory>();
            for (var week = 1; week <= MaxRegularSeasonWeek; week++)
            {
                var games = await _storageService.GetWeeklyGamesAsync(season, week, cancellationToken);
                if (games is null) continue;

                var pick = await _storageService.GetWeeklyPickAsync(season, week, cancellationToken);
                if (pick is null) continue;

                try
                {
                    var score = _scoringService.ScoreWeek(roster, pick, games);
                    history.Add(new WeekScoreHistory(week, score));
                }
                catch (ArgumentException ex)
                {
                    // A stored pick that no longer validates against the current slate (e.g. an
                    // official-line correction changed eligibility) shouldn't crash standings.
                    _logger.LogWarning(ex, "Skipping week {Week} in standings: pick no longer validates", week);
                }
            }

            var seasonScore = ScoreSeasonSafely(history.Select(h => h.Score).ToList());

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new StandingsResponse(seasonScore, history), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting standings");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve standings" }, cancellationToken);
            return errorResponse;
        }
    }

    // IScoringService.ScoreSeason takes Max()/Min() over weeks filtered to IsGoingPerf/IsOfer,
    // which throws on an empty filtered sequence - normal early in a season before any week is a
    // going-perfect or an ofer week. Prefer the real implementation when it succeeds (it's the
    // pinned/golden-fixture-tested path); only fall back to an equivalent null-safe aggregation
    // when the season has no qualifying week yet.
    private SeasonScore ScoreSeasonSafely(List<WeeklyScore> weeks)
    {
        if (weeks.Count == 0)
            return new SeasonScore(new PoolTotals(0, 0, 0, 0), new TierWinCounts(), null, null, Array.Empty<int>());

        try
        {
            return _scoringService.ScoreSeason(weeks, Array.Empty<decimal>());
        }
        catch (InvalidOperationException)
        {
            var regular = weeks.Sum(w => w.Pools.RegularSeason);
            var dog = weeks.Sum(w => w.Pools.Dog);
            var tiers = weeks.Aggregate(new TierWinCounts(), (total, week) => total + week.WinsByTier);
            var goingPerfPotentials = weeks.Where(w => w.IsGoingPerf).Select(w => w.WeekPotential).ToList();
            var oferPotentials = weeks.Where(w => w.IsOfer).Select(w => w.WeekPotential).ToList();
            return new SeasonScore(
                new PoolTotals(regular + dog, dog, regular, 0),
                tiers,
                goingPerfPotentials.Count > 0 ? goingPerfPotentials.Max() : null,
                oferPotentials.Count > 0 ? oferPotentials.Min() : null,
                weeks.SelectMany(w => w.WinningDogMargins).OrderDescending().ToList());
        }
    }
}
