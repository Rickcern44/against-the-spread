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

public class WeekDetailFunctionTests
{
    [Fact]
    public async Task GetWeekDetail_WhenGamesAndPickStored_ReturnsOkWithScoreAndUntrackedCorrectionStatus()
    {
        var storage = Substitute.For<IStorageService>();
        var scoring = Substitute.For<IScoringService>();
        var games = new[] { new WeeklyGame("g1", 1, "PHI", "DAL", 3.5m, null) };
        var roster = new SeasonRoster(2026, NflTeamDirectory.AllTeams.Take(9));
        var pick = new WeeklyPick(1, new[] { roster.Teams[0].Id, roster.Teams[1].Id, roster.Teams[2].Id }, Array.Empty<DogPick>(), 0);
        var score = new WeeklyScore(6m, 0m, 0m, new PoolTotals(6m, 0m, 6m, 0m), new TierWinCounts(), false, false, false, null, true, false, Array.Empty<int>());

        storage.GetWeeklyGamesAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(games);
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(roster);
        storage.GetWeeklyPickAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(pick);
        scoring.ScoreWeek(roster, pick, games).Returns(score);

        var function = new WeekDetailFunction(Substitute.For<ILogger<WeekDetailFunction>>(), storage, scoring);
        var (request, response) = CreateRequest();

        var result = await function.GetWeekDetail(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Games").GetArrayLength().Should().Be(1);
        document.RootElement.GetProperty("Score").GetProperty("StarterPoints").GetDecimal().Should().Be(6m);
        document.RootElement.GetProperty("Correction").GetProperty("HasTrackedHistory").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("Correction").GetProperty("Message").GetString().Should().Contain("isn't persisted");
    }

    [Fact]
    public async Task GetWeekDetail_WhenNoGamesStored_ReturnsNotFoundWithWeekNotPulled()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<WeeklyGame>?)null);
        var function = new WeekDetailFunction(Substitute.For<ILogger<WeekDetailFunction>>(), storage, Substitute.For<IScoringService>());
        var (request, response) = CreateRequest();

        var result = await function.GetWeekDetail(request, 9, CancellationToken.None);

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
