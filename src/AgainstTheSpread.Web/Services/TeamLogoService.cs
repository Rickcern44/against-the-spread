using System.Text.Json;

namespace AgainstTheSpread.Web.Services;

/// <summary>
/// Service for resolving team logos with fault-tolerant fallback behavior
/// </summary>
public class TeamLogoService : ITeamLogoService
{
    private readonly ILogger<TeamLogoService> _logger;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, string> _teamLogoMapping;
    private const string LogoBasePath = "/images/logos/ncaa/";
    private bool _isInitialized = false;
    private Task? _initializationTask;

    public TeamLogoService(ILogger<TeamLogoService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _teamLogoMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initialize the service by loading the team logo mapping from the JSON file
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
            _logger.LogInformation("TeamLogoService: Starting initialization");

            // Blazor WebAssembly - load via HTTP
            var json = await _httpClient.GetStringAsync("/team-logo-mapping.json");

            var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (mapping != null)
            {
                foreach (var kvp in mapping)
                {
                    _teamLogoMapping[kvp.Key] = kvp.Value;
                }
                _logger.LogInformation("Loaded {Count} team logo mappings", _teamLogoMapping.Count);
            }

            _isInitialized = true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load team logo mappings due to HTTP error. Logos will not be displayed.");
            _isInitialized = true; // Don't keep retrying
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse team logo mappings JSON. Logos will not be displayed.");
            _isInitialized = true; // Don't keep retrying
        }
    }

    public string? GetLogoUrl(string? teamName)
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
        if (_teamLogoMapping.TryGetValue(teamName, out var logoId))
        {
            return $"{LogoBasePath}{logoId}.png";
        }

        // Try to find a partial match
        var partialMatch = _teamLogoMapping.Keys.FirstOrDefault(key =>
            key.Contains(teamName, StringComparison.OrdinalIgnoreCase) ||
            teamName.Contains(key, StringComparison.OrdinalIgnoreCase));

        if (partialMatch != null && _teamLogoMapping.TryGetValue(partialMatch, out logoId))
        {
            return $"{LogoBasePath}{logoId}.png";
        }

        return null;
    }

    public bool HasLogo(string? teamName)
    {
        return GetLogoUrl(teamName) != null;
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
