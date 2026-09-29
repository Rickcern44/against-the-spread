using System.Text.Json;
using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Services;

/// <summary>
/// Regression coverage for a real bug found while wiring the live app API: SeasonRoster and
/// WeeklyPick are plain classes whose constructor takes IEnumerable&lt;T&gt; while their
/// properties expose IReadOnlyList&lt;T&gt;. System.Text.Json's default parameterized-constructor
/// deserialization requires an exact type match and throws InvalidOperationException on read,
/// even though writing succeeds - so StorageServiceTests's substitute-based tests never caught
/// this: only a real serialize-then-deserialize round trip does.
/// </summary>
public class SeasonModelJsonConvertersTests
{
    private static readonly JsonSerializerOptions Options = new(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    {
        Converters = { new SeasonRosterJsonConverter(), new WeeklyPickJsonConverter() }
    };

    [Fact]
    public void SeasonRoster_RoundTripsThroughJson()
    {
        var roster = new SeasonRoster(2026, NflTeamDirectory.AllTeams.Take(9));

        var json = JsonSerializer.Serialize(roster, Options);
        var deserialized = JsonSerializer.Deserialize<SeasonRoster>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.Season.Should().Be(2026);
        deserialized.Teams.Select(t => t.Id).Should().BeEquivalentTo(roster.Teams.Select(t => t.Id));
    }

    [Fact]
    public void WeeklyPick_RoundTripsThroughJson()
    {
        var pick = new WeeklyPick(3, new[] { "PHI", "KC", "BUF" }, new[] { new DogPick("g1", "DAL") }, dogAllowance: 1);

        var json = JsonSerializer.Serialize(pick, Options);
        var deserialized = JsonSerializer.Deserialize<WeeklyPick>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.Week.Should().Be(3);
        deserialized.StarterTeamIds.Should().BeEquivalentTo(pick.StarterTeamIds);
        deserialized.DogPicks.Should().BeEquivalentTo(pick.DogPicks);
        deserialized.DogAllowance.Should().Be(1);
    }

    [Fact]
    public void WithoutConverters_DeserializationThrows_DocumentingWhyTheConverterExists()
    {
        var pick = new WeeklyPick(1, new[] { "PHI", "KC", "BUF" }, new[] { new DogPick("g1", "DAL") });
        var json = JsonSerializer.Serialize(pick);

        var act = () => JsonSerializer.Deserialize<WeeklyPick>(json);

        act.Should().Throw<InvalidOperationException>();
    }
}
