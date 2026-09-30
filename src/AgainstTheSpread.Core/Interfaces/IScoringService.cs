using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

public interface IScoringService
{
    WeeklyScore ScoreWeek(SeasonRoster roster, WeeklyPick pick, IEnumerable<WeeklyGame> games);
    decimal ScorePlayoffPick(SeasonRoster roster, PlayoffPick pick);
    SeasonScore ScoreSeason(IEnumerable<WeeklyScore> weeklyScores, IEnumerable<decimal> playoffScores);
}
