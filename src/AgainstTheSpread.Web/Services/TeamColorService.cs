using System.Text.Json;
using AgainstTheSpread.Web.Models;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for resolving team colors with fault-tolerant fallback behavior
/// </summary>
public class TeamColorService : ITeamColorService
{
    private readonly ILogger<TeamColorService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, TeamColors> _teamColorMapping;
    private bool _isInitialized = false;
    private Task? _initializationTask;

    public TeamColorService(ILogger<TeamColorService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _teamColorMapping = new Dictionary<string, TeamColors>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initialize the service by loading the team color mapping from the JSON file
    /// This is called lazily on first use in Blazor WebAssembly
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        // Prevent multiple simultaneous initialization attempts
        if (_initializationTask != null)
        {
            await _initializationTask;
            return;
        }

        _initializationTask = InitializeInternalAsync();
        await _initializationTask;
    }

    private async Task InitializeInternalAsync()
    {
        try
        {
            _logger.LogInformation("TeamColorService: Starting initialization");

            // Blazor WebAssembly - load via HTTP
            var json = await _httpClient.GetStringAsync("/team-color-mapping.json");

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var mapping = JsonSerializer.Deserialize<Dictionary<string, TeamColors>>(json, options);

            if (mapping != null)
            {
                foreach (var kvp in mapping)
                {
                    _teamColorMapping[kvp.Key] = kvp.Value;
                }
                _logger.LogInformation("Loaded {Count} team color mappings", _teamColorMapping.Count);
            }

            _isInitialized = true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load team color mappings due to HTTP error. Colors will not be displayed.");
            _isInitialized = true; // Don't keep retrying
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse team color mappings JSON. Colors will not be displayed.");
            _isInitialized = true; // Don't keep retrying
        }
    }

    public TeamColors? GetTeamColors(string? teamName)
    {
        // Synchronous version for component binding - returns null if not initialized
        if (!_isInitialized)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(teamName))
        {
            return null;
        }

        // Try exact match first (case-insensitive)
        if (_teamColorMapping.TryGetValue(teamName, out var colors))
        {
            return colors;
        }

        // Try to find a partial match
        var partialMatch = _teamColorMapping.Keys.FirstOrDefault(key =>
            key.Contains(teamName, StringComparison.OrdinalIgnoreCase) ||
            teamName.Contains(key, StringComparison.OrdinalIgnoreCase));

        if (partialMatch != null && _teamColorMapping.TryGetValue(partialMatch, out colors))
        {
            return colors;
        }

        return null;
    }

    public bool HasColors(string? teamName)
    {
        return GetTeamColors(teamName) != null;
    }

    /// <summary>
    /// Initialize the service asynchronously (for Blazor WebAssembly)
    /// </summary>
    public Task InitializeAsync(HttpClient httpClient)
    {
        // Already initialized in constructor with injected HttpClient
        return EnsureInitializedAsync();
    }
}
