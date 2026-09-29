using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoints backing the Phase 4 app's <c>/api/roster</c> contract: the single coach's
/// nine drafted teams for the current season.
/// </summary>
public sealed class RosterFunction
{
    private readonly ILogger<RosterFunction> _logger;
    private readonly IStorageService _storageService;

    public RosterFunction(ILogger<RosterFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    private static int CurrentSeason() =>
        int.TryParse(Environment.GetEnvironmentVariable("NFL_SEASON"), out var season) ? season : DateTime.UtcNow.Year;

    // Writes JSON manually rather than via HttpResponseData.WriteAsJsonAsync: that extension
    // requires an ObjectSerializer registered on the worker's options, which isolated-worker
    // integration tests that mock HttpRequestData/FunctionContext directly don't configure.
    private static async Task WriteJsonAsync<T>(HttpResponseData response, T value, CancellationToken cancellationToken)
    {
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        await response.WriteStringAsync(JsonSerializer.Serialize(value), cancellationToken);
    }

    /// <summary>
    /// GET /api/roster
    /// Returns the current season's drafted roster, or 404 if one hasn't been saved yet.
    /// </summary>
    [Function("GetRoster")]
    public async Task<HttpResponseData> GetRoster(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "roster")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var season = CurrentSeason();
            var roster = await _storageService.GetRosterAsync(season, cancellationToken);

            if (roster is null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await WriteJsonAsync(notFound, new { error = "No season roster has been drafted yet." }, cancellationToken);
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new { season = roster.Season, teams = roster.Teams }, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting season roster");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve season roster" }, cancellationToken);
            return errorResponse;
        }
    }

    /// <summary>
    /// PUT /api/roster
    /// Saves the current season's roster. The request body must contain exactly nine unique team ids.
    /// </summary>
    [Function("SaveRoster")]
    public async Task<HttpResponseData> SaveRoster(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "roster")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<SaveRosterBody>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload?.TeamIds is null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = "Invalid request body" }, cancellationToken);
                return badRequest;
            }

            IReadOnlyList<Team> teams;
            try
            {
                teams = payload.TeamIds
                    .Select(id => NflTeamDirectory.AllTeams.SingleOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ArgumentException($"Unknown team id '{id}'."))
                    .ToList();
            }
            catch (ArgumentException ex)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = ex.Message }, cancellationToken);
                return badRequest;
            }

            var season = CurrentSeason();
            SeasonRoster roster;
            try
            {
                roster = new SeasonRoster(season, teams);
            }
            catch (ArgumentException ex)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = ex.Message }, cancellationToken);
                return badRequest;
            }

            await _storageService.SaveRosterAsync(roster, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new { season = roster.Season, teams = roster.Teams }, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving season roster");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to save season roster" }, cancellationToken);
            return errorResponse;
        }
    }

    private sealed class SaveRosterBody
    {
        public string[]? TeamIds { get; set; }
    }
}
