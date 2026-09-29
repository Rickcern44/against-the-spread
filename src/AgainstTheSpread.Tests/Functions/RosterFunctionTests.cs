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

public class RosterFunctionTests
{
    [Fact]
    public async Task GetRoster_WhenRosterSaved_ReturnsOkWithTeams()
    {
        var storage = Substitute.For<IStorageService>();
        var teams = NflTeamDirectory.AllTeams.Take(9).ToList();
        var roster = new SeasonRoster(2026, teams);
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(roster);
        var function = new RosterFunction(Substitute.For<ILogger<RosterFunction>>(), storage);
        var (request, response) = CreateRequest();

        var result = await function.GetRoster(request, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("teams").GetArrayLength().Should().Be(9);
    }

    [Fact]
    public async Task GetRoster_WhenNoRosterSaved_ReturnsNotFound()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetRosterAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((SeasonRoster?)null);
        var function = new RosterFunction(Substitute.For<ILogger<RosterFunction>>(), storage);
        var (request, response) = CreateRequest();

        var result = await function.GetRoster(request, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SaveRoster_WithNineUniqueTeamIds_SavesAndReturnsOk()
    {
        var storage = Substitute.For<IStorageService>();
        var function = new RosterFunction(Substitute.For<ILogger<RosterFunction>>(), storage);
        var teamIds = NflTeamDirectory.AllTeams.Take(9).Select(t => t.Id).ToArray();
        var (request, response) = CreateRequest(JsonSerializer.Serialize(new { teamIds }));

        var result = await function.SaveRoster(request, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        await storage.Received(1).SaveRosterAsync(Arg.Is<SeasonRoster>(r => r.Teams.Count == 9), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveRoster_WithWrongTeamCount_ReturnsBadRequestAndDoesNotSave()
    {
        var storage = Substitute.For<IStorageService>();
        var function = new RosterFunction(Substitute.For<ILogger<RosterFunction>>(), storage);
        var teamIds = NflTeamDirectory.AllTeams.Take(8).Select(t => t.Id).ToArray();
        var (request, response) = CreateRequest(JsonSerializer.Serialize(new { teamIds }));

        var result = await function.SaveRoster(request, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await storage.DidNotReceive().SaveRosterAsync(Arg.Any<SeasonRoster>(), Arg.Any<CancellationToken>());
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
