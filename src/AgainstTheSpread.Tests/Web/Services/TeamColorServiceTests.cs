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
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } },
            { "Michigan", new TeamColors { Primary = "#00274C", Secondary = "#FFCB05" } },
            { "Notre Dame", new TeamColors { Primary = "#0C2340", Secondary = "#C99700" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);

        // Act
        await service.InitializeAsync(httpClient);

        // Assert
        Assert.True(service.HasColors("Alabama"));
        Assert.True(service.HasColors("Michigan"));
        Assert.True(service.HasColors("Notre Dame"));
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
        Assert.False(service.HasColors("Alabama"));
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
        Assert.False(service.HasColors("Alabama"));
    }

    [Fact]
    public async Task GetTeamColors_ReturnsCorrectColors_ForExactMatch()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } },
            { "Michigan", new TeamColors { Primary = "#00274C", Secondary = "#FFCB05" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("Alabama");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#9E1B32", colors.Primary);
        Assert.Equal("#828A8F", colors.Secondary);
    }

    [Fact]
    public async Task GetTeamColors_IsCaseInsensitive()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act & Assert
        var colors1 = service.GetTeamColors("alabama");
        var colors2 = service.GetTeamColors("ALABAMA");
        var colors3 = service.GetTeamColors("AlAbAmA");

        Assert.NotNull(colors1);
        Assert.NotNull(colors2);
        Assert.NotNull(colors3);
        Assert.Equal("#9E1B32", colors1.Primary);
        Assert.Equal("#9E1B32", colors2.Primary);
        Assert.Equal("#9E1B32", colors3.Primary);
    }

    [Fact]
    public async Task GetTeamColors_ReturnsNull_ForUnknownTeam()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } }
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
            { "Alabama Crimson Tide", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } },
            { "Michigan Wolverines", new TeamColors { Primary = "#00274C", Secondary = "#FFCB05" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("Alabama");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#9E1B32", colors.Primary);
    }

    [Fact]
    public async Task GetTeamColors_FindsPartialMatch_InReverse()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("Alabama Crimson Tide");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#9E1B32", colors.Primary);
    }

    [Fact]
    public async Task GetTeamColors_PrioritizesExactMatch_OverPartialMatch()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } },
            { "Alabama Crimson Tide", new TeamColors { Primary = "#FF0000", Secondary = "#000000" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var colors = service.GetTeamColors("Alabama");

        // Assert
        Assert.NotNull(colors);
        Assert.Equal("#9E1B32", colors.Primary);
    }

    [Fact]
    public async Task HasColors_ReturnsTrue_WhenColorsExist()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act
        var hasColors = service.HasColors("Alabama");

        // Assert
        Assert.True(hasColors);
    }

    [Fact]
    public async Task HasColors_ReturnsFalse_WhenColorsDoNotExist()
    {
        // Arrange
        var mapping = new Dictionary<string, TeamColors>
        {
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } }
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
            { "Alabama", new TeamColors { Primary = "#9E1B32", Secondary = "#828A8F" } },
            { "Michigan", new TeamColors { Primary = "#00274C", Secondary = "#FFCB05" } },
            { "Ohio State", new TeamColors { Primary = "#BB0000", Secondary = "#666666" } },
            { "Georgia", new TeamColors { Primary = "#BA0C2F", Secondary = "#000000" } },
            { "Texas", new TeamColors { Primary = "#BF5700", Secondary = "#FFFFFF" } }
        };
        var httpClient = CreateMockHttpClient(mapping);
        var service = new TeamColorService(_loggerMock, httpClient);
        await service.InitializeAsync(httpClient);

        // Act & Assert
        var alabamaColors = service.GetTeamColors("Alabama");
        var michiganColors = service.GetTeamColors("Michigan");
        var ohioStateColors = service.GetTeamColors("Ohio State");
        var georgiaColors = service.GetTeamColors("Georgia");
        var texasColors = service.GetTeamColors("Texas");

        Assert.NotNull(alabamaColors);
        Assert.Equal("#9E1B32", alabamaColors.Primary);
        Assert.NotNull(michiganColors);
        Assert.Equal("#00274C", michiganColors.Primary);
        Assert.NotNull(ohioStateColors);
        Assert.Equal("#BB0000", ohioStateColors.Primary);
        Assert.NotNull(georgiaColors);
        Assert.Equal("#BA0C2F", georgiaColors.Primary);
        Assert.NotNull(texasColors);
        Assert.Equal("#BF5700", texasColors.Primary);
    }

    [Fact]
    public async Task InitializeAsync_HandlesCaseInsensitiveJson_Successfully()
    {
        // Arrange - Create JSON with lowercase property names like the actual file
        var json = @"{
            ""Alabama"": { ""primary"": ""#9E1B32"", ""secondary"": ""#828A8F"" },
            ""Michigan"": { ""primary"": ""#00274C"", ""secondary"": ""#FFCB05"" }
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
        var alabamaColors = service.GetTeamColors("Alabama");
        var michiganColors = service.GetTeamColors("Michigan");
        
        Assert.NotNull(alabamaColors);
        Assert.Equal("#9E1B32", alabamaColors.Primary);
        Assert.Equal("#828A8F", alabamaColors.Secondary);
        Assert.NotNull(michiganColors);
        Assert.Equal("#00274C", michiganColors.Primary);
        Assert.Equal("#FFCB05", michiganColors.Secondary);
    }
}
