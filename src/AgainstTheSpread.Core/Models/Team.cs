namespace AgainstTheSpread.Core.Models;

/// <summary>A season's immutable NFL team scoring identity.</summary>
public sealed record Team(
    string Id,
    string DisplayName,
    int DivisionFinish2025,
    int ByeWeek)
{
    public int PointValue => 5 - DivisionFinish2025;
    public PointTier Tier => PointValue switch
    {
        4 => PointTier.Green,
        3 => PointTier.Blue,
        2 => PointTier.Yellow,
        1 => PointTier.Red,
        _ => throw new InvalidOperationException("Division finish must be from 1 through 4.")
    };

    public string TierColorHex => Tier switch
    {
        PointTier.Green => "#00FF00",
        PointTier.Blue => "#00FFFF",
        PointTier.Yellow => "#FFFF00",
        PointTier.Red => "#FF0000",
        _ => throw new InvalidOperationException("Unknown point tier.")
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id)) throw new ArgumentException("Team id is required.", nameof(Id));
        if (string.IsNullOrWhiteSpace(DisplayName)) throw new ArgumentException("Team name is required.", nameof(DisplayName));
        if (DivisionFinish2025 is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(DivisionFinish2025));
        if (ByeWeek is < 1 or > 18) throw new ArgumentOutOfRangeException(nameof(ByeWeek));
    }
}
