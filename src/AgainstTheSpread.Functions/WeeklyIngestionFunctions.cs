using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Services;
using AgainstTheSpread.Functions.Authentication;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Timer;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>Scheduled ingestion plus authenticated manual retriggers. Workbook upload remains the official-line fallback.</summary>
public sealed class WeeklyIngestionFunctions
{
    private readonly ILogger<WeeklyIngestionFunctions> _logger;
    private readonly WeeklyIngestionService _ingestion;
    private readonly IAdminAuthorizationService _authorization;

    public WeeklyIngestionFunctions(ILogger<WeeklyIngestionFunctions> logger, WeeklyIngestionService ingestion, IAdminAuthorizationService authorization) =>
        (_logger, _ingestion, _authorization) = (logger, ingestion, authorization);

    [Function("FetchWeeklyLines")]
    public Task FetchWeeklyLinesOnSchedule([TimerTrigger("%WeeklyLinesSchedule%")] TimerInfo _, CancellationToken cancellationToken) =>
        RefreshLinesAsync(CurrentSeason(), CurrentWeek(), cancellationToken);

    [Function("FetchWeeklyResults")]
    public Task FetchWeeklyResultsOnSchedule([TimerTrigger("%WeeklyResultsSchedule%")] TimerInfo _, CancellationToken cancellationToken) =>
        RefreshResultsAsync(CurrentSeason(), CurrentWeek(), cancellationToken);

    [Function("FetchWeeklyLinesNow")]
    public Task<HttpResponseData> FetchWeeklyLinesNow([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-lines/fetch")] HttpRequestData request, CancellationToken cancellationToken) =>
        RunManuallyAsync(request, RefreshLinesAsync, cancellationToken);

    [Function("FetchWeeklyResultsNow")]
    public Task<HttpResponseData> FetchWeeklyResultsNow([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "weekly-results/fetch")] HttpRequestData request, CancellationToken cancellationToken) =>
        RunManuallyAsync(request, RefreshResultsAsync, cancellationToken);

    private async Task<HttpResponseData> RunManuallyAsync(HttpRequestData request, Func<int, int, CancellationToken, Task> refresh, CancellationToken cancellationToken)
    {
        var authorization = await _authorization.AuthorizeAsync(request, cancellationToken);
        if (authorization.Status != AdminAuthorizationStatus.Authorized)
            return await AdminAuthorizationResponses.CreateDeniedAsync(request, authorization.Status);

        if (!int.TryParse(request.Query["season"], out var season) || !int.TryParse(request.Query["week"], out var week))
        {
            var bad = request.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteAsJsonAsync(new { error = "season and week query parameters are required" }, cancellationToken);
            return bad;
        }

        try
        {
            await refresh(season, week, cancellationToken);
            var ok = request.CreateResponse(HttpStatusCode.OK);
            await ok.WriteAsJsonAsync(new { success = true, season, week }, cancellationToken);
            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weekly ingestion failed for {Season} week {Week}", season, week);
            var unavailable = request.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await unavailable.WriteAsJsonAsync(new { success = false, error = "Provider refresh failed; previously stored games remain available." }, cancellationToken);
            return unavailable;
        }
    }

    private async Task RefreshLinesAsync(int season, int week, CancellationToken cancellationToken)
    {
        await _ingestion.RefreshLinesAsync(season, week, cancellationToken);
        _logger.LogInformation("Refreshed weekly lines for {Season} week {Week}", season, week);
    }

    private async Task RefreshResultsAsync(int season, int week, CancellationToken cancellationToken)
    {
        await _ingestion.RefreshResultsAsync(season, week, cancellationToken);
        _logger.LogInformation("Refreshed weekly results for {Season} week {Week}", season, week);
    }

    private static int CurrentSeason() => int.TryParse(Environment.GetEnvironmentVariable("NFL_SEASON"), out var season) ? season : DateTime.UtcNow.Year;
    private static int CurrentWeek() => int.TryParse(Environment.GetEnvironmentVariable("NFL_WEEK"), out var week) ? week : throw new InvalidOperationException("NFL_WEEK must be configured for scheduled ingestion.");
}
