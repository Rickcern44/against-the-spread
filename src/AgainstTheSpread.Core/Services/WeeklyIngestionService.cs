using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

/// <summary>Coordinates providers without allowing a failed refresh to erase a playable stored slate.</summary>
public sealed class WeeklyIngestionService
{
    private readonly ILinesProvider _lines;
    private readonly IResultsProvider _results;
    private readonly IStorageService _storage;

    public WeeklyIngestionService(ILinesProvider lines, IResultsProvider results, IStorageService storage)
        => (_lines, _results, _storage) = (lines, results, storage);

    public async Task<IReadOnlyList<WeeklyGame>> RefreshLinesAsync(int season, int week, CancellationToken cancellationToken = default)
    {
        var games = await _lines.GetWeeklyGamesAsync(season, week, cancellationToken);
        // Preserve commissioner overrides and previously populated results across market refreshes.
        var current = await _storage.GetWeeklyGamesAsync(season, week, cancellationToken);
        var merged = games.Select(game => current?.FirstOrDefault(existing => existing.Id == game.Id) is { } existing
            ? game with { OfficialLine = existing.OfficialLine, Result = existing.Result }
            : game).ToArray();
        await _storage.SaveWeeklyGamesAsync(season, week, merged, cancellationToken);
        return merged;
    }

    public async Task<IReadOnlyList<WeeklyGame>> RefreshResultsAsync(int season, int week, CancellationToken cancellationToken = default)
    {
        var games = await _storage.GetWeeklyGamesAsync(season, week, cancellationToken)
            ?? throw new InvalidOperationException("Fetch lines before attempting results.");
        var finalScores = await _results.GetFinalResultsAsync(season, week, cancellationToken);
        var scoreByGame = finalScores.ToDictionary(x => x.GameId, StringComparer.OrdinalIgnoreCase);
        var merged = games.Select(game => scoreByGame.TryGetValue(game.Id, out var score)
            ? game with { Result = ToFavoriteResult(game, score) }
            : game).ToArray();
        await _storage.SaveWeeklyGamesAsync(season, week, merged, cancellationToken);
        return merged;
    }

    public async Task SetOfficialLineAsync(int season, int week, string gameId, decimal officialLine, CancellationToken cancellationToken = default)
    {
        if (officialLine < 0) throw new ArgumentOutOfRangeException(nameof(officialLine));
        var games = await _storage.GetWeeklyGamesAsync(season, week, cancellationToken)
            ?? throw new InvalidOperationException("No stored weekly games exist.");
        var updated = games.Select(game => string.Equals(game.Id, gameId, StringComparison.OrdinalIgnoreCase)
            ? game with { OfficialLine = officialLine }
            : game).ToArray();
        if (updated.SequenceEqual(games)) throw new KeyNotFoundException($"Game {gameId} was not found.");
        await _storage.SaveWeeklyGamesAsync(season, week, updated, cancellationToken);
    }

    private static GameResult ToFavoriteResult(WeeklyGame game, WeeklyGameResult score)
    {
        var favoriteAtHome = string.Equals(game.FavoriteTeamId, score.HomeTeamId, StringComparison.OrdinalIgnoreCase);
        var favoriteAtAway = string.Equals(game.FavoriteTeamId, score.AwayTeamId, StringComparison.OrdinalIgnoreCase);
        if (!favoriteAtHome && !favoriteAtAway) throw new InvalidOperationException($"Result teams do not match game {game.Id}.");
        return favoriteAtHome
            ? new GameResult(score.HomeScore, score.AwayScore)
            : new GameResult(score.AwayScore, score.HomeScore);
    }
}
