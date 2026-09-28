namespace AgainstTheSpread.Core.Models;

/// <summary>The coach's weekly three starters and commissioner-declared dog allowance.</summary>
public sealed class WeeklyPick
{
    public const int RequiredStarterCount = 3;

    public WeeklyPick(int week, IEnumerable<string> starterTeamIds, IEnumerable<DogPick> dogPicks, int dogAllowance = 1)
    {
        Week = week;
        StarterTeamIds = starterTeamIds?.ToList() ?? throw new ArgumentNullException(nameof(starterTeamIds));
        DogPicks = dogPicks?.ToList() ?? throw new ArgumentNullException(nameof(dogPicks));
        DogAllowance = dogAllowance;
    }

    public int Week { get; }
    public IReadOnlyList<string> StarterTeamIds { get; }
    public IReadOnlyList<DogPick> DogPicks { get; }
    public int DogAllowance { get; }

    public void Validate(SeasonRoster roster, IEnumerable<WeeklyGame> games)
    {
        if (Week is < 1 or > 18) throw new ArgumentOutOfRangeException(nameof(Week));
        if (DogAllowance < 1) throw new ArgumentOutOfRangeException(nameof(DogAllowance));
        if (StarterTeamIds.Count != RequiredStarterCount || StarterTeamIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != RequiredStarterCount)
            throw new ArgumentException("Exactly three distinct starters are required.");
        if (DogPicks.Count != DogAllowance || DogPicks.Select(p => p.GameId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != DogPicks.Count)
            throw new ArgumentException("Dog picks must exactly match the week's declared allowance and use distinct games.");

        var slate = games.Where(g => g.Week == Week).ToDictionary(g => g.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var teamId in StarterTeamIds)
        {
            var team = roster.GetTeam(teamId);
            if (team.ByeWeek == Week) throw new ArgumentException($"{team.Id} is on bye in week {Week}.");
            if (!slate.Values.Any(g => string.Equals(g.FavoriteTeamId, team.Id, StringComparison.OrdinalIgnoreCase) || string.Equals(g.UnderdogTeamId, team.Id, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"{team.Id} has no game in week {Week}.");
        }
        foreach (var dog in DogPicks)
        {
            if (!slate.TryGetValue(dog.GameId, out var game)) throw new ArgumentException($"Dog game {dog.GameId} is not in week {Week}.");
            if (!string.Equals(game.UnderdogTeamId, dog.TeamId, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("A dog pick must be the game's underdog.");
        }
    }
}

public sealed record DogPick(string GameId, string TeamId);
