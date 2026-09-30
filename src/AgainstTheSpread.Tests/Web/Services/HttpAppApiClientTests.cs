using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Web.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Web.Services;

public class HttpAppApiClientTests
{
    [Fact]
    public async Task GetWeekGamesAsync_OnSuccess_DeserializesIntoApiResponseSuccess()
    {
        var games = new[] { new WeeklyGame("g1", 1, "PHI", "DAL", 3.5m, null) };
        var teams = new[] { new Team("PHI", "Philadelphia Eagles", 1, 9), new Team("DAL", "Dallas Cowboys", 2, 10) };
        var payload = new WeekGamesResponse(1, games, teams);
        var client = new HttpAppApiClient(FakeHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(payload)));

        var result = await client.GetWeekGamesAsync(1);

        result.Succeeded.Should().BeTrue();
        result.Data!.Games.Should().ContainSingle();
    }

    [Fact]
    public async Task GetWeekGamesAsync_OnNotFoundWithProblemBody_MapsToApiResponseFailure()
    {
        var problem = new ApiProblem(ApiProblemCode.WeekNotPulled, "Week 5 has not been pulled.");
        var client = new HttpAppApiClient(FakeHttpClient(HttpStatusCode.NotFound, JsonSerializer.Serialize(problem)));

        var result = await client.GetWeekGamesAsync(5);

        result.Succeeded.Should().BeFalse();
        result.Problem!.Code.Should().Be(ApiProblemCode.WeekNotPulled);
    }

    [Fact]
    public async Task GetWeekGamesAsync_OnUnparsableErrorBody_FallsBackToGenericRetryableProblem()
    {
        var client = new HttpAppApiClient(FakeHttpClient(HttpStatusCode.InternalServerError, "not json"));

        var result = await client.GetWeekGamesAsync(1);

        result.Succeeded.Should().BeFalse();
        result.Problem!.IsRetryable.Should().BeTrue();
    }

    private static HttpClient FakeHttpClient(HttpStatusCode statusCode, string json) =>
        new(new StaticResponseHandler(statusCode, json)) { BaseAddress = new Uri("https://example.test/") };

    private sealed class StaticResponseHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
    }
}
