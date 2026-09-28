using System.Text.Json;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Tests.Services;

/// <summary>Loads the committed roster-2026.json / expected/lines-2026-week3.json fixtures into
/// domain models, shared by the starter and dog recommendation acceptance tests so both exercise
/// the exact same recorded week-3 data docs/swan-league-spec.md §7 was pinned against.</summary>
internal static class RecommendationFixtureLoader
{
    public static SeasonRoster LoadRoster()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "roster-2026.json")));
        var teams = doc.RootElement.GetProperty("teams").EnumerateArray()
            .Select(t => new Team(
                t.GetProperty("abbreviation").GetString()!,
                t.GetProperty("displayName").GetString()!,
                5 - t.GetProperty("pointValue").GetInt32(),
                t.GetProperty("byeWeek2026").GetInt32()))
            .ToList();
        return new SeasonRoster(2026, teams);
    }

    public static List<WeeklyGame> LoadWeek3Games()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "expected", "lines-2026-week3.json")));
        return doc.RootElement.GetProperty("games").EnumerateArray().Select(g =>
        {
            // shortName is "AWAY @ HOME" (or "AWAY VS HOME" for the one neutral-site game);
            // favoriteSide names which of those two slots is favored, not which physical side.
            var tokens = g.GetProperty("shortName").GetString()!.Replace(" VS ", " @ ").Split(" @ ");
            var homeIsFavorite = g.GetProperty("favoriteSide").GetString() == "home";
            var favorite = homeIsFavorite ? tokens[1] : tokens[0];
            var underdog = homeIsFavorite ? tokens[0] : tokens[1];
            var favoriteSpread = Math.Abs(g.GetProperty("favoriteSpread").GetDecimal());
            return new WeeklyGame(g.GetProperty("eventId").GetString()!, 3, favorite, underdog, favoriteSpread, null);
        }).ToList();
    }
}
