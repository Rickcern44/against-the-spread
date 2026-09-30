using AgainstTheSpread.Web.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Web.Services;

public class TeamLogoServiceTests
{
    private readonly ILogger<TeamLogoService> _loggerMock;

    public TeamLogoServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<TeamLogoService>>();
    }

    // HttpMessageHandler.SendAsync is protected internal, so NSubstitute cannot configure it
    // through its public API; a hand-written subclass is the only route.
    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory());
    }

    private HttpClient CreateMockHttpClient(Dictionary<string, string> mapping)
    {
        var json = JsonSerializer.Serialize(mapping);
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    private HttpClient CreateFailingHttpClient()
    {
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound
        });

        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    [Fact]
    public async Task InitializeAsync_LoadsMappingFile_Successfully()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" },
            { "Michigan", "130" },
            { "Notre Dame", "87" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.True(service.HasLogo("Alabama"));
        Assert.True(service.HasLogo("Michigan"));
        Assert.True(service.HasLogo("Notre Dame"));
    }

    [Fact]
    public async Task InitializeAsync_HandlesEmptyMapping_Gracefully()
    {
        // Arrange
        var mapping = new Dictionary<string, string>();
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.False(service.HasLogo("Alabama"));
    }

    [Fact]
    public async Task InitializeAsync_HandlesHttpError_Gracefully()
    {
        // Arrange
        var httpClient = CreateFailingHttpClient();
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.False(service.HasLogo("Alabama"));
    }

    [Fact]
    public async Task GetLogoUrl_ReturnsCorrectPath_ForExactMatch()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" },
            { "Michigan", "130" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("Alabama");

        // Assert
        Assert.Equal("/images/logos/ncaa/333.png", logoUrl);
    }

    [Fact]
    public async Task GetLogoUrl_IsCaseInsensitive()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act & Assert
        Assert.Equal("/images/logos/ncaa/333.png", service.GetLogoUrl("alabama"));
        Assert.Equal("/images/logos/ncaa/333.png", service.GetLogoUrl("ALABAMA"));
        Assert.Equal("/images/logos/ncaa/333.png", service.GetLogoUrl("AlAbAmA"));
    }

    [Fact]
    public async Task GetLogoUrl_ReturnsNull_ForUnknownTeam()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("Unknown Team");

        // Assert
        Assert.Null(logoUrl);
    }

    [Fact]
    public void GetLogoUrl_ReturnsNull_ForNullInput_BeforeInitialization()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, string>());
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        var logoUrl = service.GetLogoUrl(null);

        // Assert
        Assert.Null(logoUrl);
    }

    [Fact]
    public void GetLogoUrl_ReturnsNull_ForEmptyString_BeforeInitialization()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, string>());
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("");

        // Assert
        Assert.Null(logoUrl);
    }

    [Fact]
    public void GetLogoUrl_ReturnsNull_ForWhitespaceString()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, string>());
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("   ");

        // Assert
        Assert.Null(logoUrl);
    }

    [Fact]
    public async Task GetLogoUrl_FindsPartialMatch_WhenExactMatchNotFound()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama Crimson Tide", "333" },
            { "Michigan Wolverines", "130" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("Alabama");

        // Assert
        Assert.Equal("/images/logos/ncaa/333.png", logoUrl);
    }

    [Fact]
    public async Task GetLogoUrl_FindsPartialMatch_InReverse()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("Alabama Crimson Tide");

        // Assert
        Assert.Equal("/images/logos/ncaa/333.png", logoUrl);
    }

    [Fact]
    public async Task GetLogoUrl_PrioritizesExactMatch_OverPartialMatch()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" },
            { "Alabama Crimson Tide", "999" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var logoUrl = service.GetLogoUrl("Alabama");

        // Assert
        Assert.Equal("/images/logos/ncaa/333.png", logoUrl);
    }

    [Fact]
    public async Task HasLogo_ReturnsTrue_WhenLogoExists()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var hasLogo = service.HasLogo("Alabama");

        // Assert
        Assert.True(hasLogo);
    }

    [Fact]
    public async Task HasLogo_ReturnsFalse_WhenLogoDoesNotExist()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var hasLogo = service.HasLogo("Unknown Team");

        // Assert
        Assert.False(hasLogo);
    }

    [Fact]
    public void HasLogo_ReturnsFalse_ForNullInput()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, string>());
        var service = new TeamLogoService(_loggerMock, httpClient);

        // Act
        var hasLogo = service.HasLogo(null);

        // Assert
        Assert.False(hasLogo);
    }

    [Fact]
    public async Task GetLogoUrl_HandlesMultipleTeams_Correctly()
    {
        // Arrange
        var mapping = new Dictionary<string, string>
        {
            { "Alabama", "333" },
            { "Michigan", "130" },
            { "Ohio State", "194" },
            { "Georgia", "61" },
            { "Texas", "251" }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamLogoService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act & Assert
        Assert.Equal("/images/logos/ncaa/333.png", service.GetLogoUrl("Alabama"));
        Assert.Equal("/images/logos/ncaa/130.png", service.GetLogoUrl("Michigan"));
        Assert.Equal("/images/logos/ncaa/194.png", service.GetLogoUrl("Ohio State"));
        Assert.Equal("/images/logos/ncaa/61.png", service.GetLogoUrl("Georgia"));
        Assert.Equal("/images/logos/ncaa/251.png", service.GetLogoUrl("Texas"));
    }
}
