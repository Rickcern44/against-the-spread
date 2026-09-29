using System.Net;
using System.Text;
using System.Text.Json;
using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Functions;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AgainstTheSpread.Tests.Functions;

public class WeekGamesFunctionTests
{
    [Fact]
    public async Task GetWeekGames_WhenGamesStored_ReturnsOkWithGamesAndReferencedTeams()
    {
        var storage = Substitute.For<IStorageService>();
        var games = new[] { new WeeklyGame("g1", 1, "PHI", "DAL", 3.5m, null) };
        var roster = new SeasonRoster(2026, NflTeamDirectory.AllTeams.Take(9));
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(games);
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(roster);
        var function = new WeekGamesFunction(Substitute.For<ILogger<WeekGamesFunction>>(), storage);
        var (request, response) = CreateRequest();

        var result = await function.GetWeekGames(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Games").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetWeekGames_WhenNoGamesStored_ReturnsNotFoundWithWeekNotPulled()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<WeeklyGame>?)null);
        var function = new WeekGamesFunction(Substitute.For<ILogger<WeekGamesFunction>>(), storage);
        var (request, response) = CreateRequest();

        var result = await function.GetWeekGames(request, 5, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Code").GetInt32().Should().Be((int)Core.Contracts.ApiProblemCode.WeekNotPulled);
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
