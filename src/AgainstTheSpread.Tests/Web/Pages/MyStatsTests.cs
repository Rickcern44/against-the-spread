using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Pages;

public sealed class MyStatsTests : BunitContext
{
    [Fact]
    public void Stats_ShowsFourPoolsAndWeeklyHistory()
    {
        Services.AddScoped<IAppApiClient, FakeApiClient>();
        var cut = Render<AgainstTheSpread.Web.Pages.MyStats>();
        cut.Markup.Should().Contain("Main").And.Contain("Dog").And.Contain("Regular season").And.Contain("Playoff");
        cut.Markup.Should().Contain("Going Perf:").And.Contain("Week-by-week");
    }
}
