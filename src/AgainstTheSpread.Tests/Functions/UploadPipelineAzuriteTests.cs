using AgainstTheSpread.Core.Services;
using AgainstTheSpread.Functions;
using AgainstTheSpread.Functions.Authentication;
using AgainstTheSpread.Web.Services;
using Azure.Core.Serialization;
using Azure.Storage.Blobs;
using AwesomeAssertions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OfficeOpenXml;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Functions;

public sealed class UploadPipelineAzuriteTests : IDisposable
{
    private const string ConnectionString = "UseDevelopmentStorage=true";
    private readonly Process? azurite;
    private readonly string? azuriteDirectory;
    private readonly StorageService storage;
    private readonly ApiService api;

    public UploadPipelineAzuriteTests()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        if (!CanConnect(10000))
        {
            azuriteDirectory = Path.Combine(Path.GetTempPath(), $"ats-upload-pipeline-{Guid.NewGuid():N}");
            Directory.CreateDirectory(azuriteDirectory);
            azurite = Process.Start(new ProcessStartInfo
            {
                FileName = "azurite",
                Arguments = $"--silent --location \"{azuriteDirectory}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }) ?? throw new InvalidOperationException("Failed to start Azurite.");
            WaitForAzurite();
        }

        var weeklyExcel = new ExcelService();
        var bowlExcel = new BowlExcelService();
        storage = new StorageService(
            ConnectionString,
            weeklyExcel,
            bowlExcel,
            NullLogger<StorageService>.Instance);

        var authorization = new IntegrationAuthorizationService();
        var weeklyFunction = new UploadLinesFunction(
            NullLogger<UploadLinesFunction>.Instance,
            weeklyExcel,
            storage,
            authorization);
        var bowlFunction = new UploadBowlLinesFunction(
            NullLogger<UploadBowlLinesFunction>.Instance,
            bowlExcel,
            storage,
            authorization);
        var handler = new FunctionUploadHandler(weeklyFunction, bowlFunction);
        api = new ApiService(
            new HttpClient(handler) { BaseAddress = new Uri("https://integration.test/") },
            NullLogger<ApiService>.Instance);
    }

    [Fact]
    public async Task WeeklyClientUpload_ReachesRealParserAndAzuriteReadback()
    {
        const int week = 52;
        const int year = 2099;
        var workbook = CreateWeeklyWorkbook();

        await using var stream = new MemoryStream(workbook);
        var response = await api.UploadLinesAsync(week, year, stream, "weekly.xlsx", "integration-token");
        var stored = await storage.GetLinesAsync(week, year);
        var storedWorkbook = await DownloadBlob($"lines/week-{week}-{year}.xlsx");

        response.Should().NotBeNull();
        stored.Should().NotBeNull();
        stored!.Games.Should().ContainSingle();
        stored.Games[0].Favorite.Should().Be("Integration Favorite");
        storedWorkbook.Should().Equal(workbook);
    }

    [Fact]
    public async Task WeeklyClientUpload_InvalidDate_ReturnsCellErrorAndPreservesExistingBlobs()
    {
        const int week = 51;
        const int year = 2099;
        using var valid = new MemoryStream(CreateWeeklyWorkbook());
        await api.UploadLinesAsync(week, year, valid, "valid.xlsx", "integration-token");
        var originalJson = await DownloadBlob($"lines/week-{week}-{year}.json");
        var originalWorkbook = await DownloadBlob($"lines/week-{week}-{year}.xlsx");

        using var package = new ExcelPackage(new MemoryStream(CreateWeeklyWorkbook()));
        var sheet = package.Workbook.Worksheets[0];
        sheet.Cells[11, 1].Value = "Friday, September 19, 2026";
        sheet.Cells[13, 2].Value = "Another Favorite";
        sheet.Cells[13, 3].Value = -7.5;
        sheet.Cells[13, 5].Value = "Another Underdog";
        using var invalid = new MemoryStream(package.GetAsByteArray());

        var response = await api.UploadLinesAsync(week, year, invalid, "invalid.xlsx", "integration-token");

        (await DownloadBlob($"lines/week-{week}-{year}.json")).Should().Equal(originalJson);
        (await DownloadBlob($"lines/week-{week}-{year}.xlsx")).Should().Equal(originalWorkbook);
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.Message.Should().Contain("A11").And.Contain("Friday, September 19, 2026");
    }

    [Fact]
    public async Task BowlClientUpload_ReachesRealParserAndAzuriteReadback()
    {
        const int year = 2099;
        var workbook = CreateBowlWorkbook();

        await using var stream = new MemoryStream(workbook);
        var response = await api.UploadBowlLinesAsync(year, stream, "bowls.xlsx", "integration-token");
        var stored = await storage.GetBowlLinesAsync(year);
        var storedWorkbook = await DownloadBlob($"bowl-lines/bowls-{year}.xlsx");

        response.Should().NotBeNull();
        stored.Should().NotBeNull();
        stored!.Games.Should().ContainSingle();
        stored.Games[0].BowlName.Should().Be("Integration Bowl");
        storedWorkbook.Should().Equal(workbook);
    }

    public void Dispose()
    {
        if (azurite is { HasExited: false })
        {
            azurite.Kill(entireProcessTree: true);
            azurite.WaitForExit(5000);
        }
        azurite?.Dispose();
        if (azuriteDirectory != null && Directory.Exists(azuriteDirectory))
        {
            Directory.Delete(azuriteDirectory, recursive: true);
        }
    }

    private static byte[] CreateWeeklyWorkbook()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Week 52 Lines");
        sheet.Cells[3, 1].Value = "WEEK 52";
        sheet.Cells[5, 2].Value = "Favorite";
        sheet.Cells[5, 3].Value = "Line";
        sheet.Cells[5, 4].Value = "vs/at";
        sheet.Cells[5, 5].Value = "Under Dog";
        sheet.Cells[7, 1].Value = "Thursday, December 31, 2099";
        sheet.Cells[9, 2].Value = "Integration Favorite";
        sheet.Cells[9, 3].Value = -3.5;
        sheet.Cells[9, 4].Value = "vs";
        sheet.Cells[9, 5].Value = "Integration Underdog";
        return package.GetAsByteArray();
    }

    private static byte[] CreateBowlWorkbook()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Bowl Lines");
        sheet.Cells[1, 1].Value = "Bowl Name";
        sheet.Cells[1, 2].Value = "Favorite";
        sheet.Cells[1, 3].Value = "Line";
        sheet.Cells[1, 4].Value = "Under Dog";
        sheet.Cells[2, 1].Value = "Integration Bowl";
        sheet.Cells[2, 2].Value = "Integration Favorite";
        sheet.Cells[2, 3].Value = -2.5;
        sheet.Cells[2, 4].Value = "Integration Underdog";
        return package.GetAsByteArray();
    }

    private static async Task<byte[]> DownloadBlob(string name)
    {
        var blob = new BlobContainerClient(ConnectionString, "gamefiles").GetBlobClient(name);
        var content = await blob.DownloadContentAsync();
        return content.Value.Content.ToArray();
    }

    private static bool CanConnect(int port)
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync(IPAddress.Loopback, port).Wait(TimeSpan.FromMilliseconds(250));
        }
        catch
        {
            return false;
        }
    }

    private static void WaitForAzurite()
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (CanConnect(10000))
            {
                return;
            }
            Thread.Sleep(100);
        }
        throw new TimeoutException("Azurite did not start on port 10000.");
    }

    private sealed class IntegrationAuthorizationService : IAdminAuthorizationService
    {
        public Task<AdminAuthorizationResult> AuthorizeAsync(
            HttpRequestData request,
            CancellationToken cancellationToken)
        {
            var token = request.Headers.TryGetValues("X-Google-ID-Token", out var values)
                ? values.SingleOrDefault()
                : null;
            return Task.FromResult(token == "integration-token"
                ? new AdminAuthorizationResult(AdminAuthorizationStatus.Authorized, "integration@example.test")
                : new AdminAuthorizationResult(AdminAuthorizationStatus.Unauthorized));
        }
    }

    private sealed class FunctionUploadHandler : HttpMessageHandler
    {
        private readonly UploadLinesFunction weekly;
        private readonly UploadBowlLinesFunction bowl;

        public FunctionUploadHandler(UploadLinesFunction weekly, UploadBowlLinesFunction bowl)
        {
            this.weekly = weekly;
            this.bowl = bowl;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var functionRequest = await CreateRequest(request, cancellationToken);
            var response = request.RequestUri!.AbsolutePath.EndsWith("/upload-lines", StringComparison.Ordinal)
                ? await weekly.Run(functionRequest, cancellationToken)
                : await bowl.Run(functionRequest, cancellationToken);
            response.Body.Position = 0;
            using var responseBody = new MemoryStream();
            await response.Body.CopyToAsync(responseBody, cancellationToken);
            return new HttpResponseMessage(response.StatusCode)
            {
                Content = new ByteArrayContent(responseBody.ToArray())
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") }
                }
            };
        }

        private static async Task<HttpRequestData> CreateRequest(
            HttpRequestMessage source,
            CancellationToken cancellationToken)
        {
            var context = Substitute.For<FunctionContext>();
            var workerOptions = Options.Create(new WorkerOptions
            {
                Serializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            });
            var services = new ServiceCollection()
                .AddSingleton<IOptions<WorkerOptions>>(workerOptions)
                .BuildServiceProvider();
            context.InstanceServices.Returns(services);

            var functionResponse = Substitute.For<HttpResponseData>(context);
            functionResponse.Headers.Returns(new HttpHeadersCollection());
            functionResponse.Body = new MemoryStream();

            var headers = new HttpHeadersCollection();
            foreach (var header in source.Headers)
            {
                headers.Add(header.Key, header.Value);
            }
            var query = new NameValueCollection();
            foreach (var pair in source.RequestUri!.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                query[Uri.UnescapeDataString(parts[0])] = parts.Length == 2
                    ? Uri.UnescapeDataString(parts[1])
                    : string.Empty;
            }
            var body = source.Content == null
                ? Array.Empty<byte>()
                : await source.Content.ReadAsByteArrayAsync(cancellationToken);

            var functionRequest = Substitute.For<HttpRequestData>(context);
            functionRequest.Headers.Returns(headers);
            functionRequest.Query.Returns(query);
            functionRequest.Body.Returns(new MemoryStream(body));
            functionRequest.CreateResponse().Returns(functionResponse);
            return functionRequest;
        }
    }
}
