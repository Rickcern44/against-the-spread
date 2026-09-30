using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Services;

public sealed class DogRecommendationService : IDogRecommendationService
{
    public DogRecommendation Recommend(IEnumerable<WeeklyGame> games, int week, int dogAllowance = 1)
    {
        if (dogAllowance < 1) throw new ArgumentOutOfRangeException(nameof(dogAllowance));
        var slate = games?.Where(g => g.Week == week).ToList() ?? throw new ArgumentNullException(nameof(games));

        var ranked = slate
            .Select(BuildCandidate)
            .OrderByDescending(c => c.ExpectedValue)
            .ThenByDescending(c => c.WinProbability)
            .ThenBy(c => c.TeamId, StringComparer.Ordinal)
            .ToList();

        var recommended = ranked.Take(dogAllowance).Select(c => c.TeamId).ToList();
        return new DogRecommendation(week, ranked, recommended);
    }

    private static DogCandidate BuildCandidate(WeeklyGame game)
    {
        var dogSpread = game.DogSpread(game.UnderdogTeamId);
        var dogPoints = (int)decimal.Ceiling(dogSpread);

        // The dog fixture rounds win probability to 4dp BEFORE multiplying by the integer point
        // value; the starter fixture rounds the EV product itself instead. Both pipelines were
        // pinned independently in docs/swan-league-spec.md §7.4/§7.5 and the committed fixtures
        // only reproduce under their respective order of operations - this is not a stylistic
        // choice, changing it breaks the golden fixture.
        var winProbability = SpreadWinProbability.Round(SpreadWinProbability.RawProbability(-dogSpread));
        var expectedValue = Math.Round(winProbability * dogPoints, 4, MidpointRounding.AwayFromZero);

        return new DogCandidate(game.UnderdogTeamId, game.Id, game.FavoriteTeamId, dogSpread, dogPoints, winProbability, expectedValue);
    }
}
