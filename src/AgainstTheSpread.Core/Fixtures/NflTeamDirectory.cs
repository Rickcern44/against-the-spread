using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Fixtures;

/// <summary>The full 32-team pool used to build a season roster. Reference data, not scoring truth.</summary>
public static class NflTeamDirectory
{
    public static readonly IReadOnlyList<Team> AllTeams = new[]
    {
        new Team("22", "Arizona Cardinals", 3, 8), new Team("1", "Atlanta Falcons", 3, 5),
        new Team("33", "Baltimore Ravens", 2, 13), new Team("2", "Buffalo Bills", 2, 7),
        new Team("29", "Carolina Panthers", 4, 14), new Team("3", "Chicago Bears", 3, 5),
        new Team("4", "Cincinnati Bengals", 3, 10), new Team("5", "Cleveland Browns", 4, 9),
        new Team("6", "Dallas Cowboys", 2, 10), new Team("7", "Denver Broncos", 1, 12),
        new Team("8", "Detroit Lions", 2, 8), new Team("9", "Green Bay Packers", 3, 5),
        new Team("34", "Houston Texans", 1, 6), new Team("11", "Indianapolis Colts", 1, 11),
        new Team("30", "Jacksonville Jaguars", 4, 8), new Team("12", "Kansas City Chiefs", 1, 10),
        new Team("13", "Las Vegas Raiders", 3, 8), new Team("24", "Los Angeles Chargers", 2, 5),
        new Team("14", "Los Angeles Rams", 1, 8), new Team("15", "Miami Dolphins", 4, 12),
        new Team("16", "Minnesota Vikings", 4, 6), new Team("17", "New England Patriots", 1, 14),
        new Team("18", "New Orleans Saints", 4, 11), new Team("19", "New York Giants", 4, 14),
        new Team("20", "New York Jets", 3, 9), new Team("21", "Philadelphia Eagles", 1, 9),
        new Team("23", "Pittsburgh Steelers", 2, 5), new Team("25", "San Francisco 49ers", 2, 14),
        new Team("26", "Seattle Seahawks", 2, 8), new Team("27", "Tampa Bay Buccaneers", 1, 9),
        new Team("10", "Tennessee Titans", 4, 10), new Team("28", "Washington Commanders", 1, 12),
    };
}
