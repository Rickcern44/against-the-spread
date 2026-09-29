using System.Net;
using System.Text;
using System.Text.Json;
using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Functions;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace AgainstTheSpread.Tests.Functions;

public class WeekPicksFunctionTests
{
    [Fact]
    public async Task GetWeekPicks_WhenPickSaved_ReturnsOk()
    {
        var storage = Substitute.For<IStorageService>();
        var pick = new WeeklyPick(1, new[] { "PHI", "KC", "BUF" }, new[] { new DogPick("g1", "DAL") });
        storage.GetWeeklyPickAsync(Arg.Any<int>(), 1, Arg.Any<CancellationToken>()).Returns(pick);
        var function = new WeekPicksFunction(Substitute.For<ILogger<WeekPicksFunction>>(), storage);
        var (request, response) = CreateRequest();

        var result = await function.GetWeekPicks(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWeekPicks_WhenNoPickSaved_ReturnsNotFoundWithNoPicksLogged()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetWeeklyPickAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((WeeklyPick?)null);
        var function = new WeekPicksFunction(Substitute.For<ILogger<WeekPicksFunction>>(), storage);
        var (request, response) = CreateRequest();

        var result = await function.GetWeekPicks(request, 3, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Code").GetInt32().Should().Be((int)ApiProblemCode.NoPicksLogged);
    }

    [Fact]
    public async Task SaveWeekPicks_WithMatchingWeek_SavesAndReturnsOk()
    {
        var storage = Substitute.For<IStorageService>();
        var function = new WeekPicksFunction(Substitute.For<ILogger<WeekPicksFunction>>(), storage);
        var pick = new WeeklyPick(2, new[] { "PHI", "KC", "BUF" }, new[] { new DogPick("g1", "DAL") });
        var body = JsonSerializer.Serialize(new SaveWeekPicksRequest(pick));
        var (request, response) = CreateRequest(body);

        var result = await function.SaveWeekPicks(request, 2, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        await storage.Received(1).SaveWeeklyPickAsync(Arg.Any<int>(), Arg.Is<WeeklyPick>(p => p.Week == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveWeekPicks_WithMismatchedWeek_ReturnsBadRequestAndDoesNotSave()
    {
        var storage = Substitute.For<IStorageService>();
        var function = new WeekPicksFunction(Substitute.For<ILogger<WeekPicksFunction>>(), storage);
        var pick = new WeeklyPick(2, new[] { "PHI", "KC", "BUF" }, new[] { new DogPick("g1", "DAL") });
        var body = JsonSerializer.Serialize(new SaveWeekPicksRequest(pick));
        var (request, response) = CreateRequest(body);

        var result = await function.SaveWeekPicks(request, 3, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await storage.DidNotReceive().SaveWeeklyPickAsync(Arg.Any<int>(), Arg.Any<WeeklyPick>(), Arg.Any<CancellationToken>());
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
