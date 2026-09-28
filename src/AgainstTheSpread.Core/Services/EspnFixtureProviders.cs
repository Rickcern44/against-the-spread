using System.Text.Json;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

/// <summary>Recorded ESPN adapters for tests, local E2E, and deterministic development.</summary>
public sealed class EspnFixtureLinesProvider : ILinesProvider
{
    private readonly JsonDocument _fixture;
    public EspnFixtureLinesProvider(Stream fixture) => _fixture = JsonDocument.Parse(fixture);

    public Task<IReadOnlyList<WeeklyGame>> GetWeeklyGamesAsync(int season, int week, CancellationToken cancellationToken = default)
    {
        var root = _fixture.RootElement;
        if (root.GetProperty("season").GetInt32() != season || root.GetProperty("week").GetInt32() != week)
            throw new ArgumentException("The recorded odds fixture is for a different season or week.");

        var games = new List<WeeklyGame>();
        foreach (var game in root.GetProperty("games").EnumerateArray())
        {
            var odds = game.GetProperty("response").GetProperty("items")[0];
            var away = odds.GetProperty("awayTeamOdds");
            var home = odds.GetProperty("homeTeamOdds");
            var matchup = game.GetProperty("shortName").GetString()!;
            var teams = matchup.Split(new[] { " @ ", " vs " }, StringSplitOptions.None);
            if (teams.Length != 2) throw new FormatException($"Unsupported ESPN matchup: {matchup}");
            // ESPN labels neutral games "vs". The listed order remains away then home and
            // is used only to identify the side of the spread, never to infer the favourite.
            var awayId = teams[0];
            var homeId = teams[1];
            var awayFavorite = away.GetProperty("favorite").GetBoolean();
            var line = Math.Abs(awayFavorite
                ? away.GetProperty("current").GetProperty("pointSpread").GetProperty("alternateDisplayValue").GetString() is { } a ? decimal.Parse(a, System.Globalization.CultureInfo.InvariantCulture) : 0
                : home.GetProperty("current").GetProperty("pointSpread").GetProperty("alternateDisplayValue").GetString() is { } h ? decimal.Parse(h, System.Globalization.CultureInfo.InvariantCulture) : 0);
            games.Add(new WeeklyGame(game.GetProperty("eventId").GetString()!, week,
                awayFavorite ? awayId : homeId, awayFavorite ? homeId : awayId, line, null));
        }
        return Task.FromResult<IReadOnlyList<WeeklyGame>>(games);
    }
}

public sealed class EspnFixtureResultsProvider : IResultsProvider
{
    private readonly JsonDocument _fixture;
    public EspnFixtureResultsProvider(Stream fixture) => _fixture = JsonDocument.Parse(fixture);

    public Task<IReadOnlyList<WeeklyGameResult>> GetFinalResultsAsync(int season, int week, CancellationToken cancellationToken = default)
    {
        var root = _fixture.RootElement;
        if (root.GetProperty("season").GetInt32() != season || root.GetProperty("week").GetInt32() != week)
            throw new ArgumentException("The recorded scores fixture is for a different season or week.");
        var results = new List<WeeklyGameResult>();
        foreach (var game in root.GetProperty("response").GetProperty("events").EnumerateArray())
        {
            if (!game.GetProperty("status").GetProperty("type").GetProperty("completed").GetBoolean()) continue;
            var competitors = game.GetProperty("competitions")[0].GetProperty("competitors").EnumerateArray().ToArray();
            var home = competitors.Single(c => c.GetProperty("homeAway").GetString() == "home");
            var away = competitors.Single(c => c.GetProperty("homeAway").GetString() == "away");
            // Results are in actual home/away order; orchestration maps them to favorite/underdog.
            results.Add(new WeeklyGameResult(game.GetProperty("id").GetString()!,
                home.GetProperty("team").GetProperty("abbreviation").GetString()!, int.Parse(home.GetProperty("score").GetString()!),
                away.GetProperty("team").GetProperty("abbreviation").GetString()!, int.Parse(away.GetProperty("score").GetString()!)));
        }
        return Task.FromResult<IReadOnlyList<WeeklyGameResult>>(results);
    }
}
