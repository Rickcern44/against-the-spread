namespace AgainstTheSpread.Core.Models;

public sealed record TierWinCounts(int Green = 0, int Blue = 0, int Yellow = 0, int Red = 0)
{
    public int Total => Green + Blue + Yellow + Red;
    public TierWinCounts Add(PointTier tier) => tier switch
    {
        PointTier.Green => this with { Green = Green + 1 }, PointTier.Blue => this with { Blue = Blue + 1 },
        PointTier.Yellow => this with { Yellow = Yellow + 1 }, PointTier.Red => this with { Red = Red + 1 }, _ => this
    };
    public static TierWinCounts operator +(TierWinCounts left, TierWinCounts right) => new(left.Green + right.Green, left.Blue + right.Blue, left.Yellow + right.Yellow, left.Red + right.Red);
}

public sealed record PoolTotals(decimal Main, decimal Dog, decimal RegularSeason, decimal Playoff);

public sealed record WeeklyScore(
    decimal StarterPoints,
    decimal PerfectWeekBonus,
    decimal DogPoints,
    PoolTotals Pools,
    TierWinCounts WinsByTier,
    bool IsPerfectWeek,
    bool IsGoingPerf,
    bool IsOfer,
    decimal? WeekPotential,
    bool IsComplete,
    bool HasUnconfirmedDogPoints,
    IReadOnlyList<int> WinningDogMargins);

public sealed record SeasonScore(PoolTotals Pools, TierWinCounts WinsByTier, decimal? GoingPerfRecordTotal, decimal? OferRecordTotal, IReadOnlyList<int> WinningDogMargins);
