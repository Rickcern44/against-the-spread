using AgainstTheSpread.Web.Services;
using AgainstTheSpread.Web.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Tests.Web.Services;

public class TeamColorServiceTests
{
    private readonly ILogger<TeamColorService> _loggerMock;

    public TeamColorServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<TeamColorService>>();
    }

    // HttpMessageHandler.SendAsync is protected internal, so NSubstitute cannot configure it
    // through its public API; a hand-written subclass is the only route.
    private sealed class StubHttpMessageHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory());
    }

    private HttpClient CreateMockHttpClient(Dictionary<string, TeamColors> mapping)
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
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } },
            { "BAL", new TeamColors { Primary = "#241773", Secondary = "#9E7C0C" } },
            { "BUF", new TeamColors { Primary = "#0C2340", Secondary = "#C99700" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.True(service.HasColors("ARI"));
        Assert.True(service.HasColors("BAL"));
        Assert.True(service.HasColors("BUF"));
    }

    [Fact]
    public async Task InitializeAsync_HandlesEmptyMapping_Gracefully()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>();
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.False(service.HasColors("ARI"));
    }

    [Fact]
    public async Task InitializeAsync_HandlesHttpError_Gracefully()
    {
        // Arrange
        var httpClient = CreateFailingHttpClient();
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.False(service.HasColors("ARI"));
    }

    [Fact]
    public async Task GetTeamColors_ReturnsCorrectColors_ForExactMatch()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } },
            { "BAL", new TeamColors { Primary = "#241773", Secondary = "#9E7C0C" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("ARI");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#97233F", colors.Primary);
        Assert.Equal("#000000", colors.Secondary);
    }

    [Fact]
    public async Task GetTeamColors_IsCaseInsensitive()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act & Assert
        var colors1 = service.GetTeamColors("ari");
        var colors2 = service.GetTeamColors("ARI");
        var colors3 = service.GetTeamColors("ArI");

        Assert.NotNull(colors1);
        Assert.NotNull(colors2);
        Assert.NotNull(colors3);
        Assert.Equal("#97233F", colors1.Primary);
        Assert.Equal("#97233F", colors2.Primary);
        Assert.Equal("#97233F", colors3.Primary);
    }

    [Fact]
    public async Task GetTeamColors_ReturnsNull_ForUnknownTeam()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("Unknown Team");

        // Assert
        Assert.Null(colors);
    }

    [Fact]
    public void GetTeamColors_ReturnsNull_ForNullInput_BeforeInitialization()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, TeamColors>());
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        var colors = service.GetTeamColors(null);

        // Assert
        Assert.Null(colors);
    }

    [Fact]
    public void GetTeamColors_ReturnsNull_ForEmptyString_BeforeInitialization()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, TeamColors>());
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        var colors = service.GetTeamColors("");

        // Assert
        Assert.Null(colors);
    }

    [Fact]
    public void GetTeamColors_ReturnsNull_ForWhitespaceString()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, TeamColors>());
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        var colors = service.GetTeamColors("   ");

        // Assert
        Assert.Null(colors);
    }

    [Fact]
    public async Task GetTeamColors_FindsPartialMatch_WhenExactMatchNotFound()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI Cardinals", new TeamColors { Primary = "#97233F", Secondary = "#000000" } },
            { "BAL Ravens", new TeamColors { Primary = "#241773", Secondary = "#9E7C0C" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("ARI");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#97233F", colors.Primary);
    }

    [Fact]
    public async Task GetTeamColors_FindsPartialMatch_InReverse()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("ARI Cardinals");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#97233F", colors.Primary);
    }

    [Fact]
    public async Task GetTeamColors_PrioritizesExactMatch_OverPartialMatch()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } },
            { "ARI Cardinals", new TeamColors { Primary = "#FF0000", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("ARI");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#97233F", colors.Primary);
    }

    [Fact]
    public async Task HasColors_ReturnsTrue_WhenColorsExist()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var hasColors = service.HasColors("ARI");

        // Assert
        Assert.True(hasColors);
    }

    [Fact]
    public async Task HasColors_ReturnsFalse_WhenColorsDoNotExist()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var hasColors = service.HasColors("Unknown Team");

        // Assert
        Assert.False(hasColors);
    }

    [Fact]
    public void HasColors_ReturnsFalse_ForNullInput()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(new Dictionary<string, TeamColors>());
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        var hasColors = service.HasColors(null);

        // Assert
        Assert.False(hasColors);
    }

    [Fact]
    public async Task GetTeamColors_HandlesMultipleTeams_Correctly()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "ARI", new TeamColors { Primary = "#97233F", Secondary = "#000000" } },
            { "BAL", new TeamColors { Primary = "#241773", Secondary = "#9E7C0C" } },
            { "CHI", new TeamColors { Primary = "#0B162A", Secondary = "#C83803" } },
            { "DAL", new TeamColors { Primary = "#003594", Secondary = "#000000" } },
            { "GB", new TeamColors { Primary = "#203731", Secondary = "#FFB612" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act & Assert
        var ariColors = service.GetTeamColors("ARI");
        var michiganColors = service.GetTeamColors("BAL");
        var ohioStateColors = service.GetTeamColors("CHI");
        var georgiaColors = service.GetTeamColors("DAL");
        var texasColors = service.GetTeamColors("GB");

        Assert.NotNull(ariColors);
        Assert.Equal("#97233F", ariColors.Primary);
        Assert.NotNull(michiganColors);
        Assert.Equal("#241773", michiganColors.Primary);
        Assert.NotNull(ohioStateColors);
        Assert.Equal("#0B162A", ohioStateColors.Primary);
        Assert.NotNull(georgiaColors);
        Assert.Equal("#003594", georgiaColors.Primary);
        Assert.NotNull(texasColors);
        Assert.Equal("#203731", texasColors.Primary);
    }

    [Fact]
    public async Task InitializeAsync_HandlesCaseInsensitiveJson_Successfully()
    {
        // Arrange - Create JSON with lowercase property names like the actual file
        var json = @"{
            ""ARI"": { ""primary"": ""#97233F"", ""secondary"": ""#000000"" },
            ""BAL"": { ""primary"": ""#241773"", ""secondary"": ""#9E7C0C"" }
        }";
        
        var handler = new StubHttpMessageHandler(() => new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json)
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert - Verify colors are loaded correctly despite lowercase JSON properties
        var ariColors = service.GetTeamColors("ARI");
        var michiganColors = service.GetTeamColors("BAL");
        
        Assert.NotNull(ariColors);
        Assert.Equal("#97233F", ariColors.Primary);
        Assert.Equal("#000000", ariColors.Secondary);
        Assert.NotNull(michiganColors);
        Assert.Equal("#241773", michiganColors.Primary);
        Assert.Equal("#9E7C0C", michiganColors.Secondary);
    }
}
