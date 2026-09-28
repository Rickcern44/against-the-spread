using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>Obtains only completed results; scheduled games are deliberately omitted.</summary>
public interface IResultsProvider
{
    Task<IReadOnlyList<WeeklyGameResult>> GetFinalResultsAsync(int season, int week, CancellationToken cancellationToken = default);
}

/// <summary>Final score in provider-neutral team order. Orchestration converts it to favorite/underdog order.</summary>
public sealed record WeeklyGameResult(string GameId, string HomeTeamId, int HomeScore, string AwayTeamId, int AwayScore);
