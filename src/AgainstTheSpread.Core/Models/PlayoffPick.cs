namespace AgainstTheSpread.Core.Models;

public enum PlayoffRound { WildCard = 2, Divisional = 4, Conference = 6, SuperBowl = 8 }

/// <summary>A roster team's playoff appearance and outcome for one round.</summary>
public sealed record PlayoffPick(string TeamId, PlayoffRound Round, bool Won, bool QualifiedForPlayoffs = false, bool EarnedConferenceWinnerBye = false)
{
    public int RoundMultiplier => (int)Round;
}
