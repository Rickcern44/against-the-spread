namespace AgainstTheSpread.Core.Models;

/// <summary>The single coach's nine drafted teams for one season.</summary>
public sealed class SeasonRoster
{
    public const int RequiredTeamCount = 9;

    public SeasonRoster(int season, IEnumerable<Team> teams)
    {
        Season = season;
        Teams = teams?.ToList() ?? throw new ArgumentNullException(nameof(teams));
        Validate();
    }

    public int Season { get; }
    public IReadOnlyList<Team> Teams { get; }

    public Team GetTeam(string teamId) => Teams.Single(t => string.Equals(t.Id, teamId, StringComparison.OrdinalIgnoreCase));

    public bool Contains(string teamId) => Teams.Any(t => string.Equals(t.Id, teamId, StringComparison.OrdinalIgnoreCase));

    public void Validate()
    {
        if (Season < 2026) throw new ArgumentOutOfRangeException(nameof(Season));
        if (Teams.Count != RequiredTeamCount) throw new ArgumentException($"A season roster must contain exactly {RequiredTeamCount} teams.", nameof(Teams));
        if (Teams.Select(t => t.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Teams.Count)
            throw new ArgumentException("Roster team ids must be unique.", nameof(Teams));
        foreach (var team in Teams) team.Validate();
    }
}
