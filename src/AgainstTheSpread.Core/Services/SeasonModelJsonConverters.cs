using System.Text.Json;
using System.Text.Json.Serialization;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// <see cref="SeasonRoster"/> and <see cref="WeeklyPick"/> are plain classes whose single
/// constructor takes <c>IEnumerable&lt;T&gt;</c> parameters while their public properties expose
/// <c>IReadOnlyList&lt;T&gt;</c>. System.Text.Json's parameterized-constructor deserialization
/// requires each constructor parameter's declared type to exactly match its bound property's
/// declared type, so round-tripping either type through the default reflection-based converter
/// throws <see cref="InvalidOperationException"/> at read time (write time is unaffected - only
/// property values are serialized). These converters give <see cref="StorageService"/> a
/// deserialization path that actually works.
/// </summary>
public sealed class SeasonRosterJsonConverter : JsonConverter<SeasonRoster>
{
    public override SeasonRoster? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<Dto>(ref reader, options);
        return dto is null ? null : new SeasonRoster(dto.Season, dto.Teams ?? Array.Empty<Team>());
    }

    public override void Write(Utf8JsonWriter writer, SeasonRoster value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new Dto(value.Season, value.Teams), options);

    private sealed record Dto(int Season, IReadOnlyList<Team>? Teams);
}

public sealed class WeeklyPickJsonConverter : JsonConverter<WeeklyPick>
{
    public override WeeklyPick? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<Dto>(ref reader, options);
        return dto is null
            ? null
            : new WeeklyPick(dto.Week, dto.StarterTeamIds ?? Array.Empty<string>(), dto.DogPicks ?? Array.Empty<DogPick>(), dto.DogAllowance);
    }

    public override void Write(Utf8JsonWriter writer, WeeklyPick value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new Dto(value.Week, value.StarterTeamIds, value.DogPicks, value.DogAllowance), options);

    private sealed record Dto(int Week, IReadOnlyList<string>? StarterTeamIds, IReadOnlyList<DogPick>? DogPicks, int DogAllowance = 1);
}
