using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>API endpoints backing the Phase 4 app's <c>/api/weeks/{week}/picks</c> contract.</summary>
public sealed class WeekPicksFunction
{
    private readonly ILogger<WeekPicksFunction> _logger;
    private readonly IStorageService _storageService;

    public WeekPicksFunction(ILogger<WeekPicksFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
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
    /// GET /api/weeks/{week}/picks
    /// Returns the saved pick for the week, or a <see cref="ApiProblemCode.NoPicksLogged"/>
    /// problem if none has been saved yet.
    /// </summary>
    [Function("GetWeekPicks")]
    public async Task<HttpResponseData> GetWeekPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "weeks/{week:int}/picks")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        try
        {
            var season = CurrentSeason();
            var pick = await _storageService.GetWeeklyPickAsync(season, week, cancellationToken);
            if (pick is null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await WriteJsonAsync(notFound, new ApiProblem(ApiProblemCode.NoPicksLogged, $"No picks have been logged for week {week}."), cancellationToken);
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new WeekPicksResponse(pick), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting week {Week} picks", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to retrieve week picks" }, cancellationToken);
            return errorResponse;
        }
    }

    /// <summary>
    /// PUT /api/weeks/{week}/picks
    /// Saves the pick for the week. The URL week and Pick.Week must agree.
    /// </summary>
    [Function("SaveWeekPicks")]
    public async Task<HttpResponseData> SaveWeekPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "weeks/{week:int}/picks")] HttpRequestData req,
        int week,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync(cancellationToken);
            // Deserialize into a plain DTO rather than SaveWeekPicksRequest directly:
            // System.Text.Json's parameterized-constructor binding requires each constructor
            // parameter's declared type to exactly match its matched property's declared type,
            // and WeeklyPick's constructor takes IEnumerable<T> while its properties expose
            // IReadOnlyList<T> - a mismatch that fails deserialization outright.
            var dto = JsonSerializer.Deserialize<SaveWeekPicksBody>(body, ReadOptions);

            if (dto?.Pick is null || dto.Pick.StarterTeamIds is null || dto.Pick.DogPicks is null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = "Invalid request body" }, cancellationToken);
                return badRequest;
            }

            if (dto.Pick.Week != week)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = "The URL week and Pick.Week must agree." }, cancellationToken);
                return badRequest;
            }

            WeeklyPick pick;
            try
            {
                pick = new WeeklyPick(
                    dto.Pick.Week,
                    dto.Pick.StarterTeamIds,
                    dto.Pick.DogPicks.Select(d => new DogPick(d.GameId, d.TeamId)),
                    dto.Pick.DogAllowance);
            }
            catch (ArgumentException ex)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await WriteJsonAsync(badRequest, new { error = ex.Message }, cancellationToken);
                return badRequest;
            }

            var season = CurrentSeason();
            await _storageService.SaveWeeklyPickAsync(season, pick, cancellationToken);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await WriteJsonAsync(response, new WeekPicksResponse(pick), cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving week {Week} picks", week);
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await WriteJsonAsync(errorResponse, new { error = "Failed to save week picks" }, cancellationToken);
            return errorResponse;
        }
    }

    private sealed class SaveWeekPicksBody
    {
        public PickBody? Pick { get; set; }
    }

    private sealed class PickBody
    {
        public int Week { get; set; }
        public string[]? StarterTeamIds { get; set; }
        public DogPickBody[]? DogPicks { get; set; }
        public int DogAllowance { get; set; } = 1;
    }

    private sealed class DogPickBody
    {
        public string GameId { get; set; } = "";
        public string TeamId { get; set; } = "";
    }
}
