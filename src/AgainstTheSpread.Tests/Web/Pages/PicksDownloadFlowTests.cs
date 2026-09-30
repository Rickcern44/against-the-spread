using System.Net;
using System.Net.Http.Json;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Web.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AgainstTheSpread.Tests.Web.Pages;

public class PicksDownloadFlowTests : TestContext
{
    private readonly PicksApiHandler apiHandler = new();

    public PicksDownloadFlowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var httpClient = new HttpClient(apiHandler)
        {
            BaseAddress = new Uri("https://example.test/")
        };

        var logoService = Substitute.For<ITeamLogoService>();
        logoService.InitializeAsync(Arg.Any<HttpClient>())
            .Returns(Task.CompletedTask);

        var colorService = Substitute.For<ITeamColorService>();
        colorService.InitializeAsync(Arg.Any<HttpClient>())
            .Returns(Task.CompletedTask);

        Services.AddLogging();
        Services.AddSingleton(httpClient);
        Services.AddSingleton(logoService);
        Services.AddSingleton(colorService);
        Services.AddSingleton<ApiService>();
    }

    [Fact]
    public void GeneratePicks_KeepsWorkbookAvailableForUserActivatedDownload()
    {
        var cut = RenderReadyPicks();

        cut.Find("button.btn-success.btn-lg").Click();

        Assert.Empty(JSInterop.Invocations);
        var downloadButton = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Download File"));

        downloadButton.Click();

        var invocation = Assert.Single(JSInterop.Invocations);
        Assert.Equal("downloadFile", invocation.Identifier);
        Assert.Equal("iPhone User_Week_1_Picks.xlsx", invocation.Arguments[0]);
    }

    [Fact]
    public void ChangingPick_InvalidatesGeneratedWorkbook()
    {
        var cut = RenderReadyPicks();
        cut.Find("button.btn-success.btn-lg").Click();

        cut.FindAll(".card .d-grid .btn")[0].Click();
        cut.FindAll(".card .d-grid .btn")[1].Click();

        Assert.DoesNotContain(
            cut.FindAll("button"),
            button => button.TextContent.Contains("Download File"));
        Assert.Contains(
            cut.FindAll("button"),
            button => button.TextContent.Contains("Generate Your Picks"));
    }

    [Fact]
    public async Task ChangingPick_DuringGeneration_DoesNotExposeStaleWorkbook()
    {
        var cut = RenderReadyPicks();
        var postStarted = apiHandler.DelayNextPost();

        var generationTask = cut.Find("button.btn-success.btn-lg").TriggerEventAsync(
            "onclick",
            new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await postStarted;

        cut.FindAll(".card .d-grid .btn")[0].Click();
        cut.FindAll(".card .d-grid .btn")[1].Click();
        apiHandler.CompleteDelayedPost();
        await generationTask;

        Assert.DoesNotContain(
            cut.FindAll("button"),
            button => button.TextContent.Contains("Download File"));
        Assert.Contains(
            cut.FindAll("button"),
            button => button.TextContent.Contains("Generate Your Picks"));
    }

    [Fact]
    public async Task GoingBack_DuringGeneration_DiscardsCompletedWorkbook()
    {
        var cut = RenderReadyPicks();
        var postStarted = apiHandler.DelayNextPost();

        var generationTask = cut.Find("button.btn-success.btn-lg").TriggerEventAsync(
            "onclick",
            new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await postStarted;

        cut.Find("button.btn-secondary").Click();
        apiHandler.CompleteDelayedPost();
        await generationTask;
        cut.Find("button.btn-primary.btn-lg").Click();
        SelectSixFavorites(cut);

        Assert.Empty(cut.FindAll("button").Where(button => button.TextContent.Contains("Download File")));
        Assert.Contains(cut.FindAll("button"), button => button.TextContent.Contains("Generate Your Picks"));
    }

    [Fact]
    public async Task ReloadingWeek_DuringGeneration_DiscardsPreviousSessionWorkbook()
    {
        var cut = RenderReadyPicks();
        var postStarted = apiHandler.DelayNextPost();

        var generationTask = cut.Find("button.btn-success.btn-lg").TriggerEventAsync(
            "onclick",
            new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await postStarted;

        cut.Find("button.btn-secondary").Click();
        cut.Find("button.btn-primary.btn-lg").Click();
        SelectSixFavorites(cut);
        apiHandler.CompleteDelayedPost();
        await generationTask;

        Assert.Empty(cut.FindAll("button").Where(button => button.TextContent.Contains("Download File")));
        Assert.Contains(cut.FindAll("button"), button => button.TextContent.Contains("Generate Your Picks"));
    }

    private IRenderedComponent<AgainstTheSpread.Web.Pages.Picks> RenderReadyPicks()
    {
        var cut = RenderComponent<AgainstTheSpread.Web.Pages.Picks>();

        cut.Find("#userName").Change("iPhone User");
        cut.Find("#week").Change("1");
        cut.Find("button.btn-primary.btn-lg").Click();

        SelectSixFavorites(cut);

        return cut;
    }

    private static void SelectSixFavorites(IRenderedComponent<AgainstTheSpread.Web.Pages.Picks> cut)
    {
        for (var gameIndex = 0; gameIndex < 6; gameIndex++)
        {
            cut.FindAll(".card .d-grid .btn")[gameIndex * 2].Click();
        }
    }

    private sealed class PicksApiHandler : HttpMessageHandler
    {
        private TaskCompletionSource<HttpResponseMessage>? delayedPost;
        private TaskCompletionSource? postStarted;

        public Task DelayNextPost()
        {
            delayedPost = new(TaskCreationOptions.RunContinuationsAsynchronously);
            postStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return postStarted.Task;
        }

        public void CompleteDelayedPost() => delayedPost!.SetResult(WorkbookResponse());

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var pathAndQuery = request.RequestUri!.PathAndQuery;

            if (request.Method == HttpMethod.Get && pathAndQuery.StartsWith("/api/weeks"))
            {
                return Task.FromResult(JsonResponse(new
                {
                    year = DateTime.Now.Year,
                    weeks = new[] { 1 }
                }));
            }

            if (request.Method == HttpMethod.Get && pathAndQuery.StartsWith("/api/lines/1"))
            {
                var games = Enumerable.Range(1, 6)
                    .Select(index => new Game
                    {
                        Favorite = $"Favorite {index}",
                        Underdog = $"Underdog {index}",
                        Line = -3.5m,
                        VsAt = "vs",
                        GameDate = new DateTime(DateTime.Now.Year, 9, index)
                    })
                    .ToList();

                return Task.FromResult(JsonResponse(new WeeklyLines
                {
                    Week = 1,
                    Year = DateTime.Now.Year,
                    Games = games,
                    UploadedAt = DateTime.UtcNow
                }));
            }

            if (request.Method == HttpMethod.Post && pathAndQuery == "/api/picks")
            {
                if (delayedPost != null)
                {
                    postStarted!.SetResult();
                    return delayedPost.Task;
                }

                return Task.FromResult(WorkbookResponse());
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage WorkbookResponse() =>
            new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0x50, 0x4b, 0x03, 0x04 })
            };

        private static HttpResponseMessage JsonResponse<T>(T value) =>
            new(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(value)
            };
    }
}
