using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

public interface IStarterRecommendationService
{
    /// <summary>Ranks the roster's available (non-bye) teams by EV for the given week and picks
    /// the top 3, applying the perfect-week tiebreak when the third and fourth slots are within
    /// 0.05 EV of each other.</summary>
    StarterRecommendation Recommend(SeasonRoster roster, IEnumerable<WeeklyGame> games, int week);
}
