using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

public interface IDogRecommendationService
{
    /// <summary>Ranks every underdog on the week's slate by EV and returns the top `dogAllowance`
    /// as the recommended pick(s). Never filters by roster membership or a spread threshold.</summary>
    DogRecommendation Recommend(IEnumerable<WeeklyGame> games, int week, int dogAllowance = 1);
}
