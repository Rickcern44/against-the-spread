using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Service for storing and retrieving data from Azure Blob Storage
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Uploads weekly lines Excel file and parsed JSON to blob storage
    /// </summary>
    /// <param name="week">Week number</param>
    /// <param name="year">Year</param>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="weeklyLines">Parsed weekly lines data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the uploaded blob</returns>
    Task<string> UploadLinesAsync(
        int week,
        int year,
        Stream excelStream,
        WeeklyLines weeklyLines,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves parsed weekly lines for a specific week
    /// </summary>
    /// <param name="week">Week number</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WeeklyLines object or null if not found</returns>
    Task<WeeklyLines?> GetLinesAsync(
        int week,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of all available weeks that have lines uploaded
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of week numbers</returns>
    Task<List<int>> GetAvailableWeeksAsync(
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads weekly lines Excel file to blob storage
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="week">Week number</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the uploaded blob</returns>
    Task<string> UploadWeeklyLinesAsync(
        Stream excelStream,
        int week,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets bowl lines for a specific year
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BowlLines object or null if not found</returns>
    Task<BowlLines?> GetBowlLinesAsync(
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads bowl lines Excel file to blob storage
    /// </summary>
    /// <param name="excelStream">Excel file stream</param>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>URL of the uploaded blob</returns>
    Task<string> UploadBowlLinesAsync(
        Stream excelStream,
        int year,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if bowl lines are available for a specific year
    /// </summary>
    /// <param name="year">Year</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if bowl lines exist</returns>
    Task<bool> BowlLinesExistAsync(
        int year,
        CancellationToken cancellationToken = default);
}
