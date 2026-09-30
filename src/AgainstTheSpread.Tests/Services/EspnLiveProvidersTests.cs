using System.Net;
using System.Text;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Services;

public sealed class EspnLiveProvidersTests
{
    [Fact]
    public async Task Live_lines_normalize_the_espn_favorite_without_assuming_home()
    {
        using var client = Client(Scoreboard(completed: false), Summary(details: "PHI -3.5"));
        var games = await new EspnLiveLinesProvider(client).GetWeeklyGamesAsync(2026, 3);

        games.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new AgainstTheSpread.Core.Models.WeeklyGame("game-1", 3, "PHI", "CHI", 3.5m, null));
    }

    [Fact]
    public async Task Live_lines_throw_once_espn_stops_carrying_odds_for_a_game()
    {
        using var client = Client(Scoreboard(completed: true), Summary(details: null));
        var act = () => new EspnLiveLinesProvider(client).GetWeeklyGamesAsync(2026, 3);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Live_results_return_only_completed_games_in_actual_home_away_order()
    {
        using var client = Client(Scoreboard(completed: true), Summary(details: "PHI -3.5"));
        var results = await new EspnLiveResultsProvider(client).GetFinalResultsAsync(2026, 3);

        results.Should().ContainSingle().Which.Should().Be(new AgainstTheSpread.Core.Interfaces.WeeklyGameResult("game-1", "CHI", 17, "PHI", 24));
    }

    [Fact]
    public async Task Live_lines_identify_an_espn_http_rejection_in_the_error()
    {
        using var client = new HttpClient(new StatusCodeHandler(HttpStatusCode.Forbidden)) { BaseAddress = new Uri("https://example.test/") };
        var act = () => new EspnLiveLinesProvider(client).GetWeeklyGamesAsync(2026, 3);

        var exception = await act.Should().ThrowAsync<HttpRequestException>();
        exception.Which.Message.Should().Contain("scoreboard").And.Contain("403").And.Contain("User-Agent");
    }

    private static HttpClient Client(string scoreboardJson, string summaryJson) =>
        new(new RoutedResponseHandler(scoreboardJson, summaryJson)) { BaseAddress = new Uri("https://example.test/") };

    private static string Scoreboard(bool completed) =>
        $"{{\"events\":[{{\"id\":\"game-1\",\"status\":{{\"type\":{{\"completed\":{completed.ToString().ToLowerInvariant()}}}}},\"competitions\":[{{\"competitors\":[{{\"homeAway\":\"away\",\"score\":\"24\",\"team\":{{\"abbreviation\":\"PHI\"}}}},{{\"homeAway\":\"home\",\"score\":\"17\",\"team\":{{\"abbreviation\":\"CHI\"}}}}]}}]}}]}}";

    // ESPN's per-event summary endpoint: pickcenter is where the spread actually lives, and it
    // goes empty once a game's odds are no longer carried (e.g. an archived past season).
    private static string Summary(string? details) =>
        details is null ? "{\"pickcenter\":[]}" : $"{{\"pickcenter\":[{{\"details\":\"{details}\"}}]}}";

    private sealed class RoutedResponseHandler(string scoreboardJson, string summaryJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var json = request.RequestUri!.AbsolutePath.EndsWith("/summary", StringComparison.Ordinal) ? summaryJson : scoreboardJson;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class StatusCodeHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode));
    }
}
