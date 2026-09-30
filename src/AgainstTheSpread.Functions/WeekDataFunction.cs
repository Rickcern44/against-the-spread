using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>API endpoints backing the Phase 4 app's <c>/api/weeks/{week}/data</c> contract: pull
/// status plus manual re-trigger and official-line override commands.</summary>
public sealed class WeekDataFunction
{
    private readonly ILogger<WeekDataFunction> _logger;
    private readonly IStorageService _storageService;
    private readonly WeeklyIngestionService _ingestion;

    public WeekDataFunction(ILogger<WeekDataFunction> logger, IStorageService storageService, WeeklyIngestionService ingestion)
    {
        _logger = logger;
        _storageService = storageService;
        _ingestion = ingestion;
    }

    private static int CurrentSeason() =>
        int.TryParse(Environment.GetEnvironmentVariable("NFL_SEASON"), out var season) ? season : DateTime.UtcNow.Year;

    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };

    private static async Task WriteJsonAsync<T>(HttpResponseData response, T value, CancellationToken cancellationToken)
    {
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value), cancellationToken);
    }

    /// <summary>
    /// GET /api/weeks/{week}/data
    /// Reports pull status inferred from what's currently stored. Completion timestamps are not
    /// persisted separately from the games themselves, so a status inferred here (rather than
    /// just-triggered by a POST on this same request) always has a null CompletedAt.
    /// </summary>
    [Function("GetWeekData")]
    public async Task<HttpResponseData> GetWeekData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks/{week:int}/data")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        try
        {
            var season = CurrentSeason();
            var games = await _storageService.GetWeeklyGamesAsync(season, week, cancellationToken);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new WeekDataStatusResponse(week, InferLinesStatus(games), InferResultsStatus(games)), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week {Week} data status", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve week data status" }, cancellationToken);
            return errorResponse;
        }
    }

    /// <summary>
    /// POST /api/weeks/{week}/data
    /// Executes exactly one command: a lines pull, a results pull, or an official-line override.
    /// </summary>
    [Function("PostWeekData")]
    public async Task<HttpResponseData> PostWeekData(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weeks/{week:int}/data")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        var season = CurrentSeason();
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<PostWeekDataRequest>(body, ReadOptions);

            if (payload?.Pull is null && payload?.OfficialLineOverride is null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = "Exactly one of Pull or OfficialLineOverride must be supplied." }, cancellationToken);
                return badRequest;
            }

            if (payload.OfficialLineOverride is { } overrideRequest)
            {
                try
                {
                    await _ingestion.SetOfficialLineAsync(season, week, overrideRequest.GameId, overrideRequest.OfficialLine, cancellationToken);
                }
                catch (KeyNotFoundException ex)
                {
                    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                    await WriteJsonAsync(badRequest, new { error = ex.Message }, cancellationToken);
                    return badRequest;
                }
                catch (InvalidOperationException ex)
                {
                    var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                    await WriteJsonAsync(badRequest, new { error = ex.Message }, cancellationToken);
                    return badRequest;
                }

                var afterOverride = await _storageService.GetWeeklyGamesAsync(season, week, cancellationToken);
                var overrideResponse = req.CreateResponse(HttpStatusCode.OK);
                await WriteJsonAsync(overrideResponse, new WeekDataStatusResponse(week, InferLinesStatus(afterOverride), InferResultsStatus(afterOverride)), cancellationToken);
                return overrideResponse;
            }

            var pull = payload.Pull!.Value;
            try
            {
                var games = pull == DataPullKind.Lines
                    ? await _ingestion.RefreshLinesAsync(season, week, cancellationToken)
                    : await _ingestion.RefreshResultsAsync(season, week, cancellationToken);

                var now = DateTimeOffset.UtcNow;
                var lines = pull == DataPullKind.Lines ? new DataPullStatus(true, now) : InferLinesStatus(games);
                var results = pull == DataPullKind.Results ? new DataPullStatus(true, now) : InferResultsStatus(games);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await WriteJsonAsync(response, new WeekDataStatusResponse(week, lines, results), cancellationToken);
                return response;
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or JsonException)
            {
                _logger.LogError(ex, "{Pull} pull failed for {Season} week {Week}", pull, season, week);
                var current = await _storageService.GetWeeklyGamesAsync(season, week, cancellationToken);
                var problem = new ApiProblem(ApiProblemCode.ProviderUnavailable, $"{pull} pull failed; previously stored games remain available.", IsRetryable: true);
                var lines = pull == DataPullKind.Lines ? new DataPullStatus(false, null, problem) : InferLinesStatus(current);
                var results = pull == DataPullKind.Results ? new DataPullStatus(false, null, problem) : InferResultsStatus(current);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await WriteJsonAsync(response, new WeekDataStatusResponse(week, lines, results), cancellationToken);
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling week {Week} data command", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to process week data command" }, cancellationToken);
            return errorResponse;
        }
    }

    private static DataPullStatus InferLinesStatus(IReadOnlyList<WeeklyGame>? games)
    {
        if (games is null || games.Count == 0)
            return new DataPullStatus(false, null, new ApiProblem(ApiProblemCode.WeekNotPulled, "Lines have not been pulled for this week."));
        return games.All(g => g.ScoringLine.HasValue)
            ? new DataPullStatus(true, null)
            : new DataPullStatus(false, null, new ApiProblem(ApiProblemCode.WeekNotPulled, "Some games are missing a line."));
    }

    private static DataPullStatus InferResultsStatus(IReadOnlyList<WeeklyGame>? games)
    {
        if (games is null || games.Count == 0)
            return new DataPullStatus(false, null, new ApiProblem(ApiProblemCode.WeekNotPulled, "Results have not been pulled for this week."));
        return games.All(g => g.Result?.IsFinal == true)
            ? new DataPullStatus(true, null)
            : new DataPullStatus(false, null, new ApiProblem(ApiProblemCode.WeekNotPulled, "Some games do not have a final result yet."));
    }
}
