namespace AgainstTheSpread.Core.Models;

/// <summary>Eastern-time deadline rules from the league spec (§9): starters close Sunday
/// 1:00 PM ET; each dog closes at its own kickoff (see <see cref="WeeklyGame.HasKickedOff"/>).</summary>
public static class WeekDeadlines
{
    private static readonly TimeZoneInfo Eastern = ResolveEastern();

    private static TimeZoneInfo ResolveEastern()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
    }

    /// <summary>The Sunday 1:00 PM ET starter deadline for a week, derived from the earliest
    /// kickoff on the slate (assumed to be that week's Sunday, or the nearest Sunday before it
    /// when no kickoff is known).</summary>
    public static DateTimeOffset? StarterDeadline(IEnumerable<WeeklyGame> weekGames)
    {
        var earliest = weekGames.Where(g => g.Kickoff.HasValue).Select(g => g.Kickoff!.Value).OrderBy(k => k).FirstOrDefault();
        if (earliest == default) return null;

        var easternKickoff = TimeZoneInfo.ConvertTime(earliest, Eastern);
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)easternKickoff.DayOfWeek + 7) % 7;
        var sundayDate = easternKickoff.Date.AddDays(daysUntilSunday == 0 && easternKickoff.DayOfWeek != DayOfWeek.Sunday ? 7 : daysUntilSunday);
        var deadlineLocal = new DateTime(sundayDate.Year, sundayDate.Month, sundayDate.Day, 13, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(deadlineLocal, Eastern.GetUtcOffset(deadlineLocal));
    }

    public static bool StartersLocked(IEnumerable<WeeklyGame> weekGames, DateTimeOffset now)
    {
        var deadline = StarterDeadline(weekGames);
        return deadline.HasValue && now >= deadline.Value;
    }
}
