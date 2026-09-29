using System.Net;
using System.Text;
using System.Text.Json;
using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AgainstTheSpread.Functions;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AgainstTheSpread.Tests.Functions;

public class WeekRecommendationFunctionTests
{
    [Fact]
    public async Task GetWeekRecommendation_WhenRosterAndGamesExist_ReturnsRankedStartersAndDogs()
    {
        var storage = Substitute.For<IStorageService>();
        var roster = new SeasonRoster(2026, NflTeamDirectory.AllTeams.Take(9));
        var favorite = roster.Teams[0];
        var underdog = roster.Teams[1];
        var games = new[] { new WeeklyGame("g1", 1, favorite.Id, underdog.Id, 3.5m, null) };
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(roster);
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(games);

        var function = new WeekRecommendationFunction(
            Substitute.For<ILogger<WeekRecommendationFunction>>(), storage, new StarterRecommendationService(), new DogRecommendationService());
        var (request, response) = CreateRequest();

        var result = await function.GetWeekRecommendation(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Dogs").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetWeekRecommendation_WhenNoRosterSaved_ReturnsNotFoundWithRosterNotSet()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((SeasonRoster?)null);
        var function = new WeekRecommendationFunction(
            Substitute.For<ILogger<WeekRecommendationFunction>>(), storage, new StarterRecommendationService(), new DogRecommendationService());
        var (request, response) = CreateRequest();

        var result = await function.GetWeekRecommendation(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Code").GetInt32().Should().Be((int)ApiProblemCode.RosterNotSet);
    }

    private static (HttpRequestData Request, HttpResponseData Response) CreateRequest(string? body = null)
    {
        var context = Substitute.For<FunctionContext>();
        var response = Substitute.For<HttpResponseData>(context);
        response.Headers.Returns(new HttpHeadersCollection());
        response.Body = new MemoryStream();

        var request = Substitute.For<HttpRequestData>(context);
        request.CreateResponse().Returns(response);
        request.Body.Returns(new MemoryStream(Encoding.UTF8.GetBytes(body ?? "{}")));

        return (request, response);
    }
}
