using AgainstTheSpread.Core.Fixtures;
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Tests.Web;
using AgainstTheSpread.Web.Components;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace AgainstTheSpread.Tests.Web.Components;

public sealed class RosterPickerTests : MudBunitTestContext
{
    private readonly FakeApiClient client = new();

    public RosterPickerTests() => Services.AddScoped<IAppApiClient>(_ => client);

    [Fact]
    public void Render_ShowsAllThirtyTwoTeamsAndZeroSelected()
    {
        var cut = Render<RosterPicker>();

        cut.Markup.Should().Contain("0 / 9 selected");
        cut.FindAll("button.team-tile").Should().HaveCount(32);
    }

    [Fact]
    public async Task SaveAsync_WithFewerThanNineTeamsSelected_DoesNotPersistRoster()
    {
        var cut = Render<RosterPicker>();
        cut.FindAll("button.team-tile")[0].Click();

        FindSaveButton(cut).Click();

        (await client.GetSeasonRosterAsync()).Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_WithExactlyNineTeamsSelected_InvokesOnSavedAndPersists()
    {
        IReadOnlyList<string>? savedIds = null;
        var cut = Render<RosterPicker>(ps => ps.Add(
            p => p.OnSaved,
            EventCallback.Factory.Create<IReadOnlyList<string>>(this, ids => savedIds = ids)));
        // Re-query after every click: selecting a tile re-renders the tree, so a click on a stale
        // element reference from an earlier query throws UnknownEventHandlerIdException.
        for (var i = 0; i < 9; i++) cut.FindAll("button.team-tile")[i].Click();

        cut.Markup.Should().Contain("9 / 9 selected");

        FindSaveButton(cut).Click();

        savedIds.Should().NotBeNull();
        savedIds!.Count.Should().Be(9);
        (await client.GetSeasonRosterAsync()).Succeeded.Should().BeTrue();
    }

    private static AngleSharp.Dom.IElement FindSaveButton(IRenderedComponent<RosterPicker> cut) =>
        cut.FindAll("button").Single(b => !b.ClassList.Contains("team-tile"));
}
