using AgainstTheSpread.Web.Services;
using Bunit;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Forms;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;

namespace AgainstTheSpread.Tests.Web.Pages;

public class AdminAuthenticationTests : TestContext
{
    private const string ClientId =
        "520517828773-09fud86es46rrj48bosc2g5de1ubk46i.apps.googleusercontent.com";

    public AdminAuthenticationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleClientId"] = ClientId
            })
            .Build());
    }

    [Fact]
    public async Task GoogleCredential_AuthorizedByServer_ShowsAdminUiAndLogoutDisablesAutoSelect()
    {
        RegisterApi(HttpStatusCode.OK);
        var cut = RenderComponent<AgainstTheSpread.Web.Pages.Admin>();

        await cut.InvokeAsync(() => cut.Instance.HandleGoogleCredential("google-id-token"));

        cut.Markup.Should().Contain("Signed in as:");
        cut.Markup.Should().Contain("verified@example.com");
        cut.Find("button").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("verified@example.com");
            JSInterop.VerifyInvoke("googleAuth.disableAutoSelect");
        });
    }

    [Fact]
    public async Task GoogleCredential_RejectedByServer_ClearsLoginAndShowsCleanError()
    {
        RegisterApi(HttpStatusCode.Unauthorized);
        var cut = RenderComponent<AgainstTheSpread.Web.Pages.Admin>();

        await cut.InvokeAsync(() => cut.Instance.HandleGoogleCredential("expired-token"));

        cut.Markup.Should().Contain("expired or invalid");
        cut.Markup.Should().NotContain("weekInput");
        cut.WaitForAssertion(() => JSInterop.VerifyInvoke("googleAuth.disableAutoSelect"));
    }

    [Fact]
    public async Task OlderCredentialFailure_DoesNotOverwriteNewerAuthorizedSession()
    {
        var handler = new SessionRaceHandler(delayFirstIdentity: true, delayUpload: false);
        RegisterApi(handler);
        var cut = RenderComponent<AgainstTheSpread.Web.Pages.Admin>();

        var firstCredential = cut.InvokeAsync(
            () => cut.Instance.HandleGoogleCredential("token-a"));
        await handler.IdentityStarted.Task;

        await cut.InvokeAsync(
            () => cut.Instance.HandleGoogleCredential("token-b"));
        handler.CompleteFirstIdentity(HttpStatusCode.Unauthorized);
        await firstCredential;

        cut.Markup.Should().Contain("session-b@example.com");
        cut.Markup.Should().NotContain("expired or invalid");
    }

    [Fact]
    public async Task StaleUploadUnauthorized_DoesNotClearNewerAuthorizedSession()
    {
        var handler = new SessionRaceHandler(delayFirstIdentity: false, delayUpload: true);
        RegisterApi(handler);
        var cut = RenderComponent<AgainstTheSpread.Web.Pages.Admin>();

        await cut.InvokeAsync(
            () => cut.Instance.HandleGoogleCredential("token-a"));
        SetPrivateField(cut.Instance, "selectedFile", new TestBrowserFile());

        var upload = cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "UploadFile"));
        await handler.UploadStarted.Task;

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "LogoutAsync"));
        await cut.InvokeAsync(
            () => cut.Instance.HandleGoogleCredential("token-b"));
        handler.CompleteUpload(HttpStatusCode.Unauthorized);
        await upload;

        cut.Markup.Should().Contain("session-b@example.com");
        cut.Markup.Should().NotContain("expired or invalid");
    }

    [Fact]
    public async Task UploadFile_InvalidDate_ShowsCellSpecificErrorAndKeepsAdminSignedIn()
    {
        RegisterApi(new ValidationErrorHandler());
        var cut = RenderComponent<AgainstTheSpread.Web.Pages.Admin>();
        await cut.InvokeAsync(() => cut.Instance.HandleGoogleCredential("google-id-token"));
        SetPrivateField(cut.Instance, "selectedFile", new TestBrowserFile());
        cut.Render();

        cut.Find("button.btn-primary").Click();

        cut.Find(".alert-danger").TextContent.Should().Contain("C11")
            .And.Contain("Friday, September 19, 2026");
        cut.Markup.Should().Contain("weekInput");
        cut.Markup.Should().NotContain("Successfully uploaded");
    }

    private sealed class ValidationErrorHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/api/current-admin")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { email = "verified@example.com" })
                });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new { success = false, message = "Invalid date header at C11: 'Friday, September 19, 2026'. Check the weekday." })
            });
        }
    }

    private void RegisterApi(HttpStatusCode meStatus)
    {
        RegisterApi(new AdminApiHandler(meStatus));
    }

    private void RegisterApi(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.test/")
        };
        Services.AddSingleton(client);
        Services.AddSingleton<ApiService>();
    }

    private static Task InvokePrivateTask(object instance, string methodName) =>
        (Task)(instance.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, null)!);

    private static void SetPrivateField(object instance, string fieldName, object value) =>
        instance.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private sealed class SessionRaceHandler : HttpMessageHandler
    {
        private readonly bool delayFirstIdentity;
        private readonly bool delayUpload;
        private readonly TaskCompletionSource<HttpResponseMessage> firstIdentity =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<HttpResponseMessage> upload =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public SessionRaceHandler(bool delayFirstIdentity, bool delayUpload)
        {
            this.delayFirstIdentity = delayFirstIdentity;
            this.delayUpload = delayUpload;
        }

        public TaskCompletionSource IdentityStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource UploadStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteFirstIdentity(HttpStatusCode status) =>
            firstIdentity.SetResult(new HttpResponseMessage(status));

        public void CompleteUpload(HttpStatusCode status) =>
            upload.SetResult(new HttpResponseMessage(status));

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var token = request.Headers.TryGetValues("X-Google-ID-Token", out var values)
                ? values.Single()
                : null;

            if (request.RequestUri!.AbsolutePath == "/api/current-admin")
            {
                if (delayFirstIdentity && token == "token-a")
                {
                    IdentityStarted.SetResult();
                    return firstIdentity.Task;
                }

                var email = token == "token-b"
                    ? "session-b@example.com"
                    : "session-a@example.com";
                return Task.FromResult(JsonResponse(HttpStatusCode.OK, new { email }));
            }

            if (request.RequestUri.AbsolutePath == "/api/upload-lines" && delayUpload)
            {
                UploadStarted.SetResult();
                return upload.Task;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage JsonResponse<T>(HttpStatusCode status, T value) =>
            new(status)
            {
                Content = JsonContent.Create(value)
            };
    }

    private sealed class TestBrowserFile : IBrowserFile
    {
        public string Name => "lines.xlsx";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => 4;
        public string ContentType =>
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            new MemoryStream(new byte[] { 0x50, 0x4b, 0x03, 0x04 });
    }

    private sealed class AdminApiHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _meStatus;

        public AdminApiHandler(HttpStatusCode meStatus)
        {
            _meStatus = meStatus;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/api/current-admin")
            {
                var response = new HttpResponseMessage(_meStatus);
                if (_meStatus == HttpStatusCode.OK)
                {
                    response.Content = JsonContent.Create(new { email = "verified@example.com" });
                }

                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
