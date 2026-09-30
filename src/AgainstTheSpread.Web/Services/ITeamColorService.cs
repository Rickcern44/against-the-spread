using AgainstTheSpread.Web.Models;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for resolving team colors with fault-tolerant fallback behavior
/// </summary>
public interface ITeamColorService
{
    /// <summary>
    /// Initialize the service asynchronously (for Blazor WebAssembly)
    /// </summary>
    Task InitializeAsync(HttpClient httpClient);

    /// <summary>
    /// Gets the colors for a given team name
    /// </summary>
    /// <param name="teamName">The team name to look up</param>
    /// <returns>The team colors, or null if no colors are available</returns>
    TeamColors? GetTeamColors(string? teamName);

    /// <summary>
    /// Checks if colors exist for the given team name
    /// </summary>
    /// <param name="teamName">The team name to check</param>
    /// <returns>True if colors exist, false otherwise</returns>
    bool HasColors(string? teamName);
}
