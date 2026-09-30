using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Fixtures;

/// <summary>The full 32-team pool used to build a season roster. Reference data, not scoring truth.
///
/// Team ids are ESPN's canonical team abbreviations (e.g. "PHI", "KC"), not numeric ids: the live
/// ESPN scoreboard adapters (<see cref="AgainstTheSpread.Core.Services.EspnLiveLinesProvider"/> /
/// <see cref="AgainstTheSpread.Core.Services.EspnLiveResultsProvider"/>) identify teams by
/// abbreviation (see <c>EspnScoreboard.Teams</c>), and <see cref="WeeklyGame.FavoriteTeamId"/> /
/// <see cref="WeeklyGame.UnderdogTeamId"/> must match roster team ids exactly for scoring and
/// recommendation matching to work against real data. An earlier version of this table used
/// guessed numeric ids that never matched a live ESPN payload.</summary>
public static class NflTeamDirectory
{
    public static readonly IReadOnlyList<Team> AllTeams = new[]
    {
        new Team("ARI", "Arizona Cardinals", 3, 8), new Team("ATL", "Atlanta Falcons", 3, 5),
        new Team("BAL", "Baltimore Ravens", 2, 13), new Team("BUF", "Buffalo Bills", 2, 7),
        new Team("CAR", "Carolina Panthers", 4, 14), new Team("CHI", "Chicago Bears", 3, 5),
        new Team("CIN", "Cincinnati Bengals", 3, 10), new Team("CLE", "Cleveland Browns", 4, 9),
        new Team("DAL", "Dallas Cowboys", 2, 10), new Team("DEN", "Denver Broncos", 1, 12),
        new Team("DET", "Detroit Lions", 2, 8), new Team("GB", "Green Bay Packers", 3, 5),
        new Team("HOU", "Houston Texans", 1, 6), new Team("IND", "Indianapolis Colts", 1, 11),
        new Team("JAX", "Jacksonville Jaguars", 4, 8), new Team("KC", "Kansas City Chiefs", 1, 10),
        new Team("LV", "Las Vegas Raiders", 3, 8), new Team("LAC", "Los Angeles Chargers", 2, 5),
        new Team("LAR", "Los Angeles Rams", 1, 8), new Team("MIA", "Miami Dolphins", 4, 12),
        new Team("MIN", "Minnesota Vikings", 4, 6), new Team("NE", "New England Patriots", 1, 14),
        new Team("NO", "New Orleans Saints", 4, 11), new Team("NYG", "New York Giants", 4, 14),
        new Team("NYJ", "New York Jets", 3, 9), new Team("PHI", "Philadelphia Eagles", 1, 9),
        new Team("PIT", "Pittsburgh Steelers", 2, 5), new Team("SF", "San Francisco 49ers", 2, 14),
        new Team("SEA", "Seattle Seahawks", 2, 8), new Team("TB", "Tampa Bay Buccaneers", 1, 9),
        new Team("TEN", "Tennessee Titans", 4, 10), new Team("WSH", "Washington Commanders", 1, 12),
    };
}
