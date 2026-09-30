using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Models;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Models;

public class SeasonRosterTests
{
    private static IEnumerable<Team> NineTeams() => NflTeamDirectory.AllTeams.Take(SeasonRoster.RequiredTeamCount);

    [Fact]
    public void Constructor_WithExactlyNineUniqueTeams_Succeeds()
    {
        var roster = new SeasonRoster(2026, NineTeams());

        roster.Teams.Should().HaveCount(SeasonRoster.RequiredTeamCount);
        roster.Season.Should().Be(2026);
    }

    [Fact]
    public void Constructor_WithFewerThanNineTeams_ThrowsArgumentException()
    {
        var act = () => new SeasonRoster(2026, NineTeams().Take(8));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithMoreThanNineTeams_ThrowsArgumentException()
    {
        var act = () => new SeasonRoster(2026, NflTeamDirectory.AllTeams.Take(10));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithDuplicateTeamIds_ThrowsArgumentException()
    {
        var duplicated = NineTeams().Take(8).Append(NineTeams().First());

        var act = () => new SeasonRoster(2026, duplicated);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithSeasonBefore2026_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SeasonRoster(2025, NineTeams());

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Contains_KnownTeamId_ReturnsTrueCaseInsensitively()
    {
        var roster = new SeasonRoster(2026, NineTeams());
        var teamId = roster.Teams[0].Id;

        roster.Contains(teamId.ToUpperInvariant()).Should().BeTrue();
    }

    [Fact]
    public void Contains_UnknownTeamId_ReturnsFalse()
    {
        var roster = new SeasonRoster(2026, NineTeams());

        roster.Contains("not-a-real-id").Should().BeFalse();
    }
}
