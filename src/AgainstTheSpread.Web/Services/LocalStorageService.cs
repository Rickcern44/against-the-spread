using Microsoft.JSInterop;

namespace AgainstTheSpread.Web.Services;

/// <summary>Thin localStorage wrapper. Client-only persistence until a settings API contract exists.</summary>
public sealed class LocalStorageService(IJSRuntime js)
{
    public async ValueTask<string?> GetAsync(string key) =>
        await js.InvokeAsync<string?>("localStorage.getItem", key);

    public async ValueTask SetAsync(string key, string value) =>
        await js.InvokeVoidAsync("localStorage.setItem", key, value);
}
