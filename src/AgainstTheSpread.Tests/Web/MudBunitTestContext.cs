using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace AgainstTheSpread.Tests.Web;

/// <summary>
/// Shared bUnit context for pages that render MudBlazor components. Registers MudBlazor's
/// services (required for things like <c>MudAlert</c>'s <c>InternalMudLocalizer</c>) and relaxes
/// bUnit's strict JSInterop so the JS modules MudBlazor invokes for popovers, keyboard listeners,
/// resize observers, etc. don't fail tests that never exercise that behavior.
/// </summary>
/// <remarks>
/// Implements xUnit's <see cref="IAsyncLifetime"/> so the test framework tears the context down
/// with <c>DisposeAsync</c> rather than the synchronous <c>Dispose</c>: MudBlazor registers
/// scoped services (e.g. <c>KeyInterceptorService</c>, <c>PopoverService</c>) that only implement
/// <see cref="IAsyncDisposable"/>, and bUnit's synchronous disposal path throws trying to dispose
/// them any other way.
/// </remarks>
public abstract class MudBunitTestContext : BunitContext, IAsyncLifetime
{
    protected MudBunitTestContext()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        // Every MudBlazor component that opens a popover (MudSelect, MudMenu, etc.) requires a
        // MudPopoverProvider ancestor to register with, or it throws during OnInitializedAsync —
        // in the real app this lives in MainLayout.razor. RenderTree.Add wraps every subsequent
        // Render<T>()/RenderComponent<T>() call in this context with an ancestor; MudPopoverProvider
        // itself has no ChildContent, so it's wrapped via a small pass-through host that has one.
        RenderTree.Add<PopoverProviderHost>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    private sealed class PopoverProviderHost : ComponentBase
    {
        [Parameter] public RenderFragment? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.AddContent(1, ChildContent);
        }
    }
}
