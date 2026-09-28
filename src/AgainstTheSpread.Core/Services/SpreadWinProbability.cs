namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Converts a spread into a win probability via the normal CDF over expected margin. Pinned per
/// docs/swan-league-spec.md §7.1: sigma = 13.5, erf via Abramowitz &amp; Stegun 7.1.26 (.NET has no
/// built-in Erf). This is the single conversion function shared by the starter and dog engines;
/// de-vig is not a separate step, it is the normalization below.
/// </summary>
public static class SpreadWinProbability
{
    public const double Sigma = 13.5;

    /// <summary>
    /// Win probability for a team with the given expected margin (positive when favored), as a
    /// normalized pair against the opponent's complementary margin.
    /// </summary>
    public static double RawProbability(decimal margin)
    {
        var m = (double)margin;
        var pTeam = NormalCdf(m / Sigma);
        var pOpponent = NormalCdf(-m / Sigma);
        return pTeam / (pTeam + pOpponent);
    }

    /// <summary>Rounds a probability to the 4 decimal places the spec pins all assertions to.</summary>
    public static decimal Round(double probability) => Math.Round((decimal)probability, 4, MidpointRounding.AwayFromZero);

    private static double NormalCdf(double z) => 0.5 * (1 + Erf(z / Math.Sqrt(2)));

    private static double Erf(double x)
    {
        var sign = x < 0 ? -1.0 : 1.0;
        x = Math.Abs(x);
        const double a1 = 0.254829592, a2 = -0.284496736, a3 = 1.421413741, a4 = -1.453152027, a5 = 1.061405429, p = 0.3275911;
        var t = 1.0 / (1.0 + p * x);
        var polynomial = t * (a1 + t * (a2 + t * (a3 + t * (a4 + t * a5))));
        return sign * (1.0 - polynomial * Math.Exp(-x * x));
    }
}
