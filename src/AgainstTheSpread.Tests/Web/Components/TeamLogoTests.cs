using AgainstTheSpread.Web.Components;
using AgainstTheSpread.Web.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace AgainstTheSpread.Tests.Web.Components;

public class TeamLogoTests : TestContext
{
    private readonly ITeamLogoService _logoServiceMock;

    public TeamLogoTests()
    {
        _logoServiceMock = Substitute.For<ITeamLogoService>();
        Services.AddSingleton(_logoServiceMock);
    }

    [Fact]
    public void TeamLogo_RendersImage_WhenLogoExists()
    {
        // Arrange
        var teamName = "Alabama";
        var logoUrl = "/images/logos/ncaa/333.png";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert
        var img = cut.Find("img");
        Assert.NotNull(img);
        Assert.Equal(logoUrl, img.GetAttribute("src"));
        Assert.Equal($"{teamName} logo", img.GetAttribute("alt"));
    }

    [Fact]
    public void TeamLogo_DoesNotRenderImage_WhenLogoDoesNotExist()
    {
        // Arrange
        var teamName = "Unknown Team";
        _logoServiceMock.GetLogoUrl(teamName).Returns((string?)null);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void TeamLogo_DoesNotRenderImage_WhenTeamNameIsNull()
    {
        // Arrange
        _logoServiceMock.GetLogoUrl(null).Returns((string?)null);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, (string?)null));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void TeamLogo_DoesNotRenderImage_WhenTeamNameIsEmpty()
    {
        // Arrange
        _logoServiceMock.GetLogoUrl("").Returns((string?)null);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, ""));

        // Assert
        Assert.Empty(cut.Markup);
    }

    [Fact]
    public void TeamLogo_AppliesDefaultCssClass()
    {
        // Arrange
        var teamName = "Michigan";
        var logoUrl = "/images/logos/ncaa/130.png";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert
        var img = cut.Find("img");
        Assert.Contains("team-logo", img.GetAttribute("class"));
    }

    [Fact]
    public void TeamLogo_AppliesCustomCssClass()
    {
        // Arrange
        var teamName = "Michigan";
        var logoUrl = "/images/logos/ncaa/130.png";
        var customClass = "custom-logo-class";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName)
            .Add(p => p.CssClass, customClass));

        // Assert
        var img = cut.Find("img");
        Assert.Contains(customClass, img.GetAttribute("class"));
    }

    [Fact]
    public void TeamLogo_AppliesDefaultStyle()
    {
        // Arrange
        var teamName = "Ohio State";
        var logoUrl = "/images/logos/ncaa/194.png";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert
        var img = cut.Find("img");
        var style = img.GetAttribute("style");
        Assert.Contains("width: 24px", style);
        Assert.Contains("height: 24px", style);
        Assert.Contains("object-fit: contain", style);
        Assert.Contains("margin-right: 8px", style);
    }

    [Fact]
    public void TeamLogo_AppliesCustomStyle()
    {
        // Arrange
        var teamName = "Ohio State";
        var logoUrl = "/images/logos/ncaa/194.png";
        var customStyle = "width: 48px; height: 48px;";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName)
            .Add(p => p.Style, customStyle));

        // Assert
        var img = cut.Find("img");
        Assert.Equal(customStyle, img.GetAttribute("style"));
    }

    [Fact]
    public void TeamLogo_HasErrorHandling_OnImageLoadFailure()
    {
        // Arrange
        var teamName = "Georgia";
        var logoUrl = "/images/logos/ncaa/61.png";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert
        var img = cut.Find("img");
        Assert.Contains("onerror", img.OuterHtml);
        Assert.Contains("this.style.display='none'", img.GetAttribute("onerror"));
    }

    [Fact]
    public void TeamLogo_RendersMultipleInstances_Independently()
    {
        // Arrange
        var team1 = "Alabama";
        var team2 = "Michigan";
        var logo1 = "/images/logos/ncaa/333.png";
        var logo2 = "/images/logos/ncaa/130.png";

        _logoServiceMock.GetLogoUrl(team1).Returns(logo1);
        _logoServiceMock.GetLogoUrl(team2).Returns(logo2);

        // Act
        var cut1 = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, team1));
        var cut2 = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, team2));

        // Assert
        var img1 = cut1.Find("img");
        var img2 = cut2.Find("img");

        Assert.Equal(logo1, img1.GetAttribute("src"));
        Assert.Equal(logo2, img2.GetAttribute("src"));
        Assert.NotEqual(img1.GetAttribute("src"), img2.GetAttribute("src"));
    }

    [Fact]
    public void TeamLogo_CallsServiceWithCorrectTeamName()
    {
        // Arrange
        var teamName = "Texas";
        var logoUrl = "/images/logos/ncaa/251.png";
        _logoServiceMock.GetLogoUrl(teamName).Returns(logoUrl);

        // Act
        RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert - Component may call service multiple times during render
        _logoServiceMock.Received().GetLogoUrl(teamName);
    }

    [Theory]
    [InlineData("Alabama", "/images/logos/ncaa/333.png")]
    [InlineData("Michigan", "/images/logos/ncaa/130.png")]
    [InlineData("Ohio State", "/images/logos/ncaa/194.png")]
    [InlineData("Georgia", "/images/logos/ncaa/61.png")]
    [InlineData("Texas", "/images/logos/ncaa/251.png")]
    public void TeamLogo_RendersCorrectly_ForVariousTeams(string teamName, string expectedUrl)
    {
        // Arrange
        _logoServiceMock.GetLogoUrl(teamName).Returns(expectedUrl);

        // Act
        var cut = RenderComponent<TeamLogo>(parameters => parameters
            .Add(p => p.TeamName, teamName));

        // Assert
        var img = cut.Find("img");
        Assert.Equal(expectedUrl, img.GetAttribute("src"));
        Assert.Equal($"{teamName} logo", img.GetAttribute("alt"));
    }
}
