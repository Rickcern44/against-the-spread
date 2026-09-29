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

public class StandingsFunctionTests
{
    [Fact]
    public async Task GetStandings_ScoresWeeksWithBothGamesAndPicks_AndSkipsWeeksWithNoPick()
    {
        var storage = Substitute.For<IStorageService>();
        var teams = NflTeamDirectory.AllTeams.Take(9).ToList();
        var roster = new SeasonRoster(2026, teams);
        var ari = teams[0]; var atl = teams[1]; var bal = teams[2]; var buf = teams[3]; var car = teams[4]; var chi = teams[5];

        var week1Games = new[]
        {
            new WeeklyGame("g1", 1, ari.Id, atl.Id, 3m, null, new GameResult(24, 20)),
            new WeeklyGame("g2", 1, bal.Id, buf.Id, 2m, null, new GameResult(21, 17)),
            new WeeklyGame("g3", 1, car.Id, chi.Id, 2.5m, null, new GameResult(20, 24)),
        };
        var week1Pick = new WeeklyPick(1, new[] { ari.Id, bal.Id, car.Id }, new[] { new DogPick("g1", atl.Id) });

        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(roster);
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(week1Games);
        storage.GetWeeklyPickAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(week1Pick);
        // Week 2 has games stored but no saved pick yet - should be skipped, not scored as zero.
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), 2, Arg.Any<CancellationToken>()).Returns(week1Games.Select(g => g with { Id = g.Id + "-w2", Week = 2 }).ToArray());
        storage.GetWeeklyPickAsync(Arg.Any<int>(), 2, Arg.Any<CancellationToken>()).Returns((WeeklyPick?)null);

        var function = new StandingsFunction(Substitute.For<ILogger<StandingsFunction>>(), storage, new ScoringService());
        var (request, response) = CreateRequest();

        var result = await function.GetStandings(request, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("WeeklyHistory").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetStandings_WhenNoRosterSaved_ReturnsNotFoundWithRosterNotSet()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((SeasonRoster?)null);
        var function = new StandingsFunction(Substitute.For<ILogger<StandingsFunction>>(), storage, new ScoringService());
        var (request, response) = CreateRequest();

        var result = await function.GetStandings(request, CancellationToken.None);

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
