using AgainstTheSpread.Core.Contracts;
using AgainstTheSpread.Core.Fixtures;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Fixtures;

public class FakeApiClientRosterTests
{
    [Fact]
    public async Task GetSeasonRosterAsync_FreshInstance_ReturnsRosterNotSet()
    {
        var client = new FakeApiClient();

        var result = await client.GetSeasonRosterAsync();

        result.Succeeded.Should().BeFalse();
        result.Problem!.Code.Should().Be(ApiProblemCode.RosterNotSet);
    }

    [Fact]
    public async Task SaveSeasonRosterAsync_WithNineUniqueTeams_PersistsAndIsReturnedByGet()
    {
        var client = new FakeApiClient();
        var teamIds = NflTeamDirectory.AllTeams.Take(9).Select(t => t.Id).ToList();

        var saveResult = await client.SaveSeasonRosterAsync(new SaveSeasonRosterRequest(teamIds));
        var getResult = await client.GetSeasonRosterAsync();

        saveResult.Succeeded.Should().BeTrue();
        getResult.Succeeded.Should().BeTrue();
        getResult.Data!.Teams.Select(t => t.Id).Should().BeEquivalentTo(teamIds);
    }

    [Fact]
    public async Task SaveSeasonRosterAsync_WithWrongTeamCount_ThrowsArgumentException()
    {
        var client = new FakeApiClient();
        var teamIds = NflTeamDirectory.AllTeams.Take(8).Select(t => t.Id).ToList();

        var act = async () => await client.SaveSeasonRosterAsync(new SaveSeasonRosterRequest(teamIds));

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
