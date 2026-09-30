using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Service for storing and retrieving data from Azure Blob Storage
/// MVP: Admin manually uploads files, this service only reads them
/// </summary>
public class StorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly IExcelService _excelService;
    private readonly IBowlExcelService? _bowlExcelService;
    private readonly ILogger<StorageService> _logger;
    private const string ContainerName = "gamefiles";
    private const string LinesFolder = "lines";
    private const string BowlLinesFolder = "bowl-lines";

    public StorageService(string connectionString, IExcelService excelService, ILogger<StorageService> logger)
        : this(connectionString, excelService, null, logger)
    {
    }

    public StorageService(string connectionString, IExcelService excelService, IBowlExcelService? bowlExcelService, ILogger<StorageService> logger)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        _excelService = excelService;
        _bowlExcelService = bowlExcelService;
        _logger = logger;
    }

    /// <summary>
    /// Uploads weekly lines Excel file and parsed JSON to blob storage
    /// Note: In MVP, admin uploads manually via script, not via this method
    /// This is kept for potential future use or testing
    /// </summary>
    public async Task<string> UploadLinesAsync(
        int week,
        int year,
        Stream excelStream,
        WeeklyLines weeklyLines,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Upload Excel file
        var excelBlobName = $"{LinesFolder}/week-{week}-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Upload parsed JSON for fast API access
        var jsonBlobName = $"{LinesFolder}/week-{week}-{year}.json";
        var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

        var json = JsonSerializer.Serialize(weeklyLines, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await jsonBlobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true,
            cancellationToken: cancellationToken);

        return excelBlobClient.Uri.ToString();
    }

    /// <summary>
    /// Retrieves parsed weekly lines for a specific week
    /// Reads from JSON blob for performance (admin script creates this during upload)
    /// </summary>
    public async Task<WeeklyLines?> GetLinesAsync(
        int week,
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonBlobName = $"{LinesFolder}/week-{week}-{year}.json";
            var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

            if (!await jsonBlobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var download = await jsonBlobClient.DownloadContentAsync(cancellationToken);
            var json = download.Value.Content.ToString();

            return JsonSerializer.Deserialize<WeeklyLines>(json);
        }
        catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("not found"))
        {
            return null;
        }
    }

    /// <summary>
    /// Gets list of all available weeks that have lines uploaded
    /// Scans the lines folder for JSON files
    /// </summary>
    public async Task<List<int>> GetAvailableWeeksAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var weeks = new List<int>();

        try
        {
            var prefix = $"{LinesFolder}/week-";
            _logger.LogInformation("GetAvailableWeeksAsync called for year {Year}", year);
            _logger.LogDebug("Searching with prefix: {Prefix}", prefix);
            
            int totalBlobs = 0;
            int matchingBlobs = 0;
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync(
                traits: BlobTraits.None,
                states: BlobStates.None,
                prefix: prefix,
                cancellationToken: cancellationToken))
            {
                totalBlobs++;
                _logger.LogDebug("Found blob: {BlobName}", blobItem.Name);
                
                // Extract week number from blob name like "lines/week-1-2024.json"
                var fileName = blobItem.Name;
                if (!fileName.EndsWith($"-{year}.json"))
                {
                    _logger.LogDebug("Skipping {FileName} - doesn't end with -{Year}.json", fileName, year);
                    continue;
                }
                
                matchingBlobs++;
                _logger.LogDebug("Blob {FileName} matches year {Year}", fileName, year);

                var parts = fileName.Split('-');
                _logger.LogDebug("Split parts: [{Parts}]", string.Join(", ", parts));
                
                if (parts.Length >= 2 && int.TryParse(parts[1], out int week))
                {
                    _logger.LogDebug("Extracted week number: {Week}", week);
                    weeks.Add(week);
                }
                else
                {
                    _logger.LogWarning("Failed to parse week from parts[1]: {Part}", parts.Length >= 2 ? parts[1] : "N/A");
                }
            }

            _logger.LogInformation("Total blobs found: {TotalBlobs}, Matching blobs: {MatchingBlobs}, Weeks extracted: [{Weeks}]", 
                totalBlobs, matchingBlobs, string.Join(", ", weeks));

            weeks.Sort();
            return weeks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in GetAvailableWeeksAsync for year {Year}", year);
            return new List<int>();
        }
    }

    /// <summary>
    /// Uploads weekly lines Excel file to blob storage
    /// Parses the Excel and creates JSON cache for API performance
    /// </summary>
    public async Task<string> UploadWeeklyLinesAsync(
        Stream excelStream,
        int week,
        int year,
        CancellationToken cancellationToken = default)
    {
        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Parse the Excel file
        excelStream.Position = 0;
        var weeklyLines = await _excelService.ParseWeeklyLinesAsync(excelStream, week, year, cancellationToken);

        // Upload Excel file
        var excelBlobName = $"{LinesFolder}/week-{week}-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Upload parsed JSON for fast API access
        var jsonBlobName = $"{LinesFolder}/week-{week}-{year}.json";
        var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

        var json = JsonSerializer.Serialize(weeklyLines, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await jsonBlobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true,
            cancellationToken: cancellationToken);

        return excelBlobClient.Uri.ToString();
    }

    /// <summary>
    /// Gets bowl lines for a specific year
    /// </summary>
    public async Task<BowlLines?> GetBowlLinesAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonBlobName = $"{BowlLinesFolder}/bowls-{year}.json";
            var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

            if (!await jsonBlobClient.ExistsAsync(cancellationToken))
            {
                return null;
            }

            var download = await jsonBlobClient.DownloadContentAsync(cancellationToken);
            var json = download.Value.Content.ToString();

            return JsonSerializer.Deserialize<BowlLines>(json);
        }
        catch (Exception ex) when (ex.Message.Contains("404") || ex.Message.Contains("not found"))
        {
            return null;
        }
    }

    /// <summary>
    /// Uploads bowl lines Excel file to blob storage
    /// </summary>
    public async Task<string> UploadBowlLinesAsync(
        Stream excelStream,
        int year,
        CancellationToken cancellationToken = default)
    {
        if (_bowlExcelService == null)
        {
            throw new InvalidOperationException("BowlExcelService not configured");
        }

        // Ensure container exists
        await _containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        // Parse the Excel file
        excelStream.Position = 0;
        var bowlLines = await _bowlExcelService.ParseBowlLinesAsync(excelStream, year, cancellationToken);

        // Upload Excel file
        var excelBlobName = $"{BowlLinesFolder}/bowls-{year}.xlsx";
        var excelBlobClient = _containerClient.GetBlobClient(excelBlobName);

        excelStream.Position = 0;
        await excelBlobClient.UploadAsync(
            excelStream,
            overwrite: true,
            cancellationToken: cancellationToken);

        // Upload parsed JSON for fast API access
        var jsonBlobName = $"{BowlLinesFolder}/bowls-{year}.json";
        var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

        var json = JsonSerializer.Serialize(bowlLines, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await jsonBlobClient.UploadAsync(
            BinaryData.FromString(json),
            overwrite: true,
            cancellationToken: cancellationToken);

        return excelBlobClient.Uri.ToString();
    }

    /// <summary>
    /// Checks if bowl lines are available for a specific year
    /// </summary>
    public async Task<bool> BowlLinesExistAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jsonBlobName = $"{BowlLinesFolder}/bowls-{year}.json";
            var jsonBlobClient = _containerClient.GetBlobClient(jsonBlobName);

            return await jsonBlobClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check bowl lines existence for year {Year}", year);
            return false;
        }
    }
}
