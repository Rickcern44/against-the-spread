using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;

namespace AgainstTheSpread.Tests.Services;

public class DogRecommendationServiceTests
{
    private readonly IDogRecommendationService _service = new DogRecommendationService();

    [Fact]
    public void Recommend_ReproducesWeek3GoldenOrdering_AllSixteenDogs()
    {
        var games = RecommendationFixtureLoader.LoadWeek3Games();

        var result = _service.Recommend(games, week: 3);

        result.Ranking.Should().HaveCount(16);
        result.Ranking.Select(c => c.TeamId).Should().Equal(
            "WSH", "ARI", "MIA", "LAC", "NYJ", "ATL", "CHI", "LV", "PIT", "CLE", "TEN", "DAL", "NE", "IND", "LAR", "MIN");
        result.Ranking.Select(c => c.WinProbability).Should().Equal(
            0.2645m, 0.2893m, 0.2294m, 0.302m, 0.302m, 0.3694m, 0.3977m, 0.3977m, 0.3977m, 0.4265m, 0.4265m, 0.4121m, 0.4121m, 0.4558m, 0.4558m, 0.4558m);
        result.Ranking.Select(c => c.ExpectedValue).Should().Equal(
            2.3805m, 2.3144m, 2.294m, 2.114m, 2.114m, 1.847m, 1.5908m, 1.5908m, 1.5908m, 1.2795m, 1.2795m, 1.2363m, 1.2363m, 0.9116m, 0.9116m, 0.9116m);

        result.RecommendedDogs.Should().Equal("WSH");
    }

    [Fact]
    public void Recommend_BiggestSpreadOnTheBoardDoesNotWinOnPointsAlone()
    {
        // docs/swan-league-spec.md §7.5: MIA +10 is the largest spread in the week-3 slate but
        // ranks third - a ranker that sorts by spread instead of EV fails this.
        var games = RecommendationFixtureLoader.LoadWeek3Games();

        var result = _service.Recommend(games, week: 3);

        var ranking = result.Ranking.ToList();
        var miamiRank = ranking.Single(c => c.TeamId == "MIA");
        miamiRank.DogSpread.Should().Be(10.0m);
        ranking.OrderByDescending(c => c.DogSpread).First().TeamId.Should().Be("MIA");
        ranking.IndexOf(miamiRank).Should().Be(2); // rank 3, zero-indexed
    }

    [Fact]
    public void Recommend_HalfPointBoundaryOutranksWholeNumberAtIdenticalPoints()
    {
        // CLE/TEN +2.5 pay the same ceil'd 3 points as DAL/NE +3, but at a better win probability,
        // so they must rank above them despite fewer raw spread points.
        var games = RecommendationFixtureLoader.LoadWeek3Games();

        var result = _service.Recommend(games, week: 3);
        var ranking = result.Ranking.ToList();

        var cleRank = ranking.IndexOf(ranking.Single(c => c.TeamId == "CLE"));
        var dalRank = ranking.IndexOf(ranking.Single(c => c.TeamId == "DAL"));
        cleRank.Should().BeLessThan(dalRank);
    }

    [Fact]
    public void Recommend_BreaksExactEvTiesByAbbreviationAscending()
    {
        var games = RecommendationFixtureLoader.LoadWeek3Games();

        var result = _service.Recommend(games, week: 3);

        var threeWayTie = result.Ranking.Where(c => c.TeamId is "CHI" or "LV" or "PIT").ToList();
        threeWayTie.Select(c => c.ExpectedValue).Distinct().Should().ContainSingle();
        threeWayTie.Select(c => c.TeamId).Should().Equal("CHI", "LV", "PIT");
    }

    [Fact]
    public void Recommend_DoubleDogWeekTakesTopDogAllowanceRows()
    {
        var games = RecommendationFixtureLoader.LoadWeek3Games();

        var result = _service.Recommend(games, week: 3, dogAllowance: 2);

        result.RecommendedDogs.Should().Equal("WSH", "ARI");
    }
}
