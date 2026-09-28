using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>Obtains a normalized weekly slate. Provider-specific fields must not escape this boundary.</summary>
public interface ILinesProvider
{
    Task<IReadOnlyList<WeeklyGame>> GetWeeklyGamesAsync(int season, int week, CancellationToken cancellationToken = default);
}
