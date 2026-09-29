using System.Net;
using System.Text;
using System.Text.Json;
using AgainstTheSpread.Core.Contracts;
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

public class WeekDataFunctionTests
{
    [Fact]
    public async Task GetWeekData_WhenNoGamesStored_ReportsBothPullsNotPulled()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<WeeklyGame>?)null);
        var ingestion = new WeeklyIngestionService(Substitute.For<ILinesProvider>(), Substitute.For<IResultsProvider>(), storage);
        var function = new WeekDataFunction(Substitute.For<ILogger<WeekDataFunction>>(), storage, ingestion);
        var (request, response) = CreateRequest();

        var result = await function.GetWeekData(request, 5, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Lines").GetProperty("Succeeded").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("Results").GetProperty("Succeeded").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PostWeekData_LinesPull_WhenProviderFails_ReturnsRetryableProviderUnavailable()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<WeeklyGame>?)null);
        var lines = Substitute.For<ILinesProvider>();
        lines.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<WeeklyGame>>>(_ => throw new InvalidOperationException("boom"));
        var ingestion = new WeeklyIngestionService(lines, Substitute.For<IResultsProvider>(), storage);
        var function = new WeekDataFunction(Substitute.For<ILogger<WeekDataFunction>>(), storage, ingestion);
        var body = JsonSerializer.Serialize(new PostWeekDataRequest(Pull: DataPullKind.Lines));
        var (request, response) = CreateRequest(body);

        var result = await function.PostWeekData(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Lines").GetProperty("Succeeded").GetBoolean().Should().BeFalse();
        document.RootElement.GetProperty("Lines").GetProperty("Problem").GetProperty("IsRetryable").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PostWeekData_LinesPull_WhenProviderSucceeds_SavesAndReportsSuccess()
    {
        var storage = Substitute.For<IStorageService>();
        storage.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyList<WeeklyGame>?)null);
        var games = new[] { new WeeklyGame("g1", 1, "PHI", "DAL", 3m, null) };
        var lines = Substitute.For<ILinesProvider>();
        lines.GetWeeklyGamesAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(games);
        var ingestion = new WeeklyIngestionService(lines, Substitute.For<IResultsProvider>(), storage);
        var function = new WeekDataFunction(Substitute.For<ILogger<WeekDataFunction>>(), storage, ingestion);
        var body = JsonSerializer.Serialize(new PostWeekDataRequest(Pull: DataPullKind.Lines));
        var (request, response) = CreateRequest(body);

        var result = await function.PostWeekData(request, 1, CancellationToken.None);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        await storage.Received(1).SaveWeeklyGamesAsync(Arg.Any<int>(), 1, Arg.Any<IReadOnlyList<WeeklyGame>>(), Arg.Any<CancellationToken>());
        response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(response.Body);
        document.RootElement.GetProperty("Lines").GetProperty("Succeeded").GetBoolean().Should().BeTrue();
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
