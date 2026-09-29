using AgainstTheSpread.Core.Interfaces;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Scoped cache of whether the current season roster has been drafted, so the onboarding gate
/// doesn't re-hit the API on every navigation. One instance per app load (Blazor WASM has a
/// single DI scope for the app's lifetime), reset only by <see cref="MarkRosterSaved"/> after
/// a successful save.
/// </summary>
public sealed class RosterState(IAppApiClient api)
{
    private bool? hasRoster;

    public async Task<bool> HasRosterAsync(CancellationToken cancellationToken = default)
    {
        if (hasRoster is null)
        {
            var result = await api.GetSeasonRosterAsync(cancellationToken);
            hasRoster = result.Succeeded;
        }

        return hasRoster.Value;
    }

    public void MarkRosterSaved() => hasRoster = true;
}
