namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for resolving team logos with fault-tolerant fallback behavior
/// </summary>
public interface ITeamLogoService
{
    /// <summary>
    /// Initialize the service asynchronously (for Blazor WebAssembly)
    /// </summary>
    Task InitializeAsync(HttpClient httpClient);

    /// <summary>
    /// Gets the logo URL for a given team name
    /// </summary>
    /// <param name="teamName">The team name to look up</param>
    /// <returns>The logo URL path, or null if no logo is available</returns>
    string? GetLogoUrl(string? teamName);

    /// <summary>
    /// Checks if a logo exists for the given team name
    /// </summary>
    /// <param name="teamName">The team name to check</param>
    /// <returns>True if a logo exists, false otherwise</returns>
    bool HasLogo(string? teamName);
}
