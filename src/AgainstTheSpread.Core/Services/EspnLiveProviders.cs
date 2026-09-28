using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Live ESPN scoreboard adapters. ESPN-specific payload details intentionally stay here;
/// callers consume only the provider-neutral weekly game contracts.
/// </summary>
public sealed class EspnLiveLinesProvider : ILinesProvider
{
    private static readonly Regex Spread = new(@"^(?<team>[A-Za-z]{2,3})\s+(?<line>[+-]?\d+(?:\.\d+)?)$", RegexOptions.Compiled);
    private readonly HttpClient _http;

    public EspnLiveLinesProvider(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<WeeklyGame>> GetWeeklyGamesAsync(int season, int week, CancellationToken cancellationToken = default)
    {
        using var document = await EspnScoreboard.GetAsync(_http, season, week, cancellationToken);
        var games = new List<WeeklyGame>();
        foreach (var game in EspnScoreboard.Events(document.RootElement))
        {
            var (home, away) = EspnScoreboard.Teams(game);
            var details = game.GetProperty("competitions")[0].GetProperty("odds")[0].GetProperty("details").GetString();
            var match = details is null ? null : Spread.Match(details);
            if (match is null or { Success: false } || !decimal.TryParse(match.Groups["line"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var line))
                throw new InvalidOperationException($"ESPN did not supply a usable point spread for game {game.GetProperty("id").GetString()}.");

            var favorite = match.Groups["team"].Value.ToUpperInvariant();
            if (!string.Equals(favorite, home.Id, StringComparison.OrdinalIgnoreCase) && !string.Equals(favorite, away.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"ESPN spread favorite {favorite} does not match the game teams.");

            var underdog = string.Equals(favorite, home.Id, StringComparison.OrdinalIgnoreCase) ? away.Id : home.Id;
            games.Add(new WeeklyGame(game.GetProperty("id").GetString()!, week, favorite, underdog, Math.Abs(line), null));
        }
        return games;
    }
}

public sealed class EspnLiveResultsProvider : IResultsProvider
{
    private readonly HttpClient _http;

    public EspnLiveResultsProvider(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<WeeklyGameResult>> GetFinalResultsAsync(int season, int week, CancellationToken cancellationToken = default)
    {
        using var document = await EspnScoreboard.GetAsync(_http, season, week, cancellationToken);
        var results = new List<WeeklyGameResult>();
        foreach (var game in EspnScoreboard.Events(document.RootElement))
        {
            if (!game.GetProperty("status").GetProperty("type").GetProperty("completed").GetBoolean()) continue;
            var (home, away) = EspnScoreboard.Teams(game);
            results.Add(new WeeklyGameResult(game.GetProperty("id").GetString()!, home.Id, home.Score, away.Id, away.Score));
        }
        return results;
    }
}

internal static class EspnScoreboard
{
    private const string BaseUrl = "https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard";

    public static async Task<JsonDocument> GetAsync(HttpClient http, int season, int week, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}?limit=100&dates={season}&seasontype=2&week={week}";
        return await http.GetFromJsonAsync<JsonDocument>(url, cancellationToken)
            ?? throw new InvalidOperationException("ESPN returned an empty scoreboard response.");
    }

    public static IEnumerable<JsonElement> Events(JsonElement root) => root.GetProperty("events").EnumerateArray();

    public static (ScoreboardTeam Home, ScoreboardTeam Away) Teams(JsonElement game)
    {
        var competitors = game.GetProperty("competitions")[0].GetProperty("competitors").EnumerateArray();
        var home = competitors.Single(c => c.GetProperty("homeAway").GetString() == "home");
        var away = competitors.Single(c => c.GetProperty("homeAway").GetString() == "away");
        return (Read(home), Read(away));
    }

    private static ScoreboardTeam Read(JsonElement competitor) => new(
        competitor.GetProperty("team").GetProperty("abbreviation").GetString()!.ToUpperInvariant(),
        int.TryParse(competitor.GetProperty("score").GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var score) ? score : 0);

    internal sealed record ScoreboardTeam(string Id, int Score);
}
