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
        using var client = Client(Scoreboard(completed: false));
        var games = await new EspnLiveLinesProvider(client).GetWeeklyGamesAsync(2026, 3);

        games.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new AgainstTheSpread.Core.Models.WeeklyGame("game-1", 3, "PHI", "CHI", 3.5m, null));
    }

    [Fact]
    public async Task Live_results_return_only_completed_games_in_actual_home_away_order()
    {
        using var client = Client(Scoreboard(completed: true));
        var results = await new EspnLiveResultsProvider(client).GetFinalResultsAsync(2026, 3);

        results.Should().ContainSingle().Which.Should().Be(new AgainstTheSpread.Core.Interfaces.WeeklyGameResult("game-1", "CHI", 17, "PHI", 24));
    }

    private static HttpClient Client(string json) => new(new StaticResponseHandler(json)) { BaseAddress = new Uri("https://example.test/") };

    private static string Scoreboard(bool completed) =>
        $"{{\"events\":[{{\"id\":\"game-1\",\"status\":{{\"type\":{{\"completed\":{completed.ToString().ToLowerInvariant()}}}}},\"competitions\":[{{\"odds\":[{{\"details\":\"PHI -3.5\"}}],\"competitors\":[{{\"homeAway\":\"away\",\"score\":\"24\",\"team\":{{\"abbreviation\":\"PHI\"}}}},{{\"homeAway\":\"home\",\"score\":\"17\",\"team\":{{\"abbreviation\":\"CHI\"}}}}]}}]}}]}}";

    private sealed class StaticResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
