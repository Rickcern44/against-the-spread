using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using OfficeOpenXml;

namespace AgainstTheSpread.Core.Services;

/// <summary>
/// Service for parsing and generating Excel files for bowl games.
/// </summary>
public class BowlExcelService : IBowlExcelService
{
    public BowlExcelService()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Parses the bowl lines Excel file.
    /// Expected format based on "Bowl Lines" template.
    /// </summary>
    public async Task<BowlLines> ParseBowlLinesAsync(Stream excelStream, int? year = null, CancellationToken cancellationToken = default)
    {
        using var package = new ExcelPackage(excelStream);
        var worksheet = package.Workbook.Worksheets[0];

        var bowlLines = new BowlLines
        {
            Games = new List<BowlGame>(),
            UploadedAt = DateTime.UtcNow,
            Year = year ?? DateTime.UtcNow.Year
        };

        // Find header row - look for "Bowl Name" or similar
        int headerRow = 0;
        int bowlNameCol = 0;
        int favoriteCol = 0;
        int lineCol = 0;
        int underdogCol = 0;
        int dateCol = 0;

        for (int row = 1; row <= 20; row++)
        {
            for (int col = 1; col <= 15; col++)
            {
                var cellValue = worksheet.Cells[row, col].Text?.Trim();
                if (cellValue != null && 
                    cellValue.Contains("Bowl", StringComparison.OrdinalIgnoreCase) &&
                    cellValue.Contains("Name", StringComparison.OrdinalIgnoreCase))
                {
                    headerRow = row;
                    bowlNameCol = col;
                }
                else if (cellValue?.Equals("Favorite", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (headerRow == 0) headerRow = row;
                    favoriteCol = col;
                }
                else if (cellValue?.Equals("Line", StringComparison.OrdinalIgnoreCase) == true)
                {
                    lineCol = col;
                }
                else if (cellValue?.Contains("Under", StringComparison.OrdinalIgnoreCase) == true)
                {
                    underdogCol = col;
                }
                else if (cellValue?.Contains("Date", StringComparison.OrdinalIgnoreCase) == true)
                {
                    dateCol = col;
                }
            }
            if (headerRow > 0 && favoriteCol > 0 && lineCol > 0 && underdogCol > 0) break;
        }

        if (favoriteCol == 0 || lineCol == 0 || underdogCol == 0)
        {
            throw new FormatException("Could not find required columns in Bowl Lines file.");
        }

        // Parse games
        int gameNumber = 0;
        var endRow = worksheet.Dimension?.End.Row ?? headerRow;
        for (int row = headerRow + 1; row <= endRow; row++)
        {
            var favoriteValue = worksheet.Cells[row, favoriteCol].Text?.Trim();
            if (string.IsNullOrEmpty(favoriteValue)) continue;

            var lineText = worksheet.Cells[row, lineCol].Text?.Trim();
            var underdog = worksheet.Cells[row, underdogCol].Text?.Trim();
            var bowlName = bowlNameCol > 0 ? worksheet.Cells[row, bowlNameCol].Text?.Trim() : $"Bowl Game {gameNumber + 1}";
            var dateText = dateCol > 0 ? worksheet.Cells[row, dateCol].Text?.Trim() : null;

            if (string.IsNullOrEmpty(lineText) || string.IsNullOrEmpty(underdog))
                continue;

            if (!decimal.TryParse(lineText, out decimal line))
                continue;

            gameNumber++;
            DateTime gameDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(dateText) && DateTime.TryParse(dateText, out DateTime parsedDate))
            {
                gameDate = parsedDate;
            }

            var game = new BowlGame
            {
                GameNumber = gameNumber,
                BowlName = bowlName ?? $"Bowl Game {gameNumber}",
                Favorite = favoriteValue,
                Line = line,
                Underdog = underdog,
                GameDate = gameDate
            };

            bowlLines.Games.Add(game);
        }

        if (bowlLines.Games.Count == 0)
        {
            throw new FormatException("No bowl games found in Excel file.");
        }

        return await Task.FromResult(bowlLines);
    }

    /// <summary>
    /// Generates an Excel file with user's bowl picks in the template format.
    /// Format based on "Bowl Template" with columns:
    /// - Game info
    /// - Winner against spread
    /// - Confidence points
    /// - Outright winner
    /// </summary>
    public async Task<byte[]> GenerateBowlPicksExcelAsync(BowlUserPicks userPicks, CancellationToken cancellationToken = default)
    {
        if (!userPicks.IsValid())
        {
            throw new ArgumentException($"Invalid bowl picks: {userPicks.GetValidationError()}", nameof(userPicks));
        }

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Bowl Picks");

        // Row 1: User name in yellow cell
        worksheet.Cells[1, 1].Value = "Name:";
        worksheet.Cells[1, 2].Value = userPicks.Name;
        worksheet.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        worksheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);

        // Row 2: Empty
        
        // Row 3: Headers
        worksheet.Cells[3, 1].Value = "Game #";
        worksheet.Cells[3, 2].Value = "Winner vs Spread";
        worksheet.Cells[3, 3].Value = "Confidence";
        worksheet.Cells[3, 4].Value = "Outright Winner";

        // Bold headers
        worksheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;

        // Rows 4+: Picks sorted by game number
        var sortedPicks = userPicks.Picks.OrderBy(p => p.GameNumber).ToList();
        int row = 4;
        foreach (var pick in sortedPicks)
        {
            worksheet.Cells[row, 1].Value = pick.GameNumber;
            worksheet.Cells[row, 2].Value = pick.SpreadPick;
            worksheet.Cells[row, 3].Value = pick.ConfidencePoints;
            worksheet.Cells[row, 4].Value = pick.OutrightWinner;
            row++;
        }

        // Validation row (sum of confidence points)
        int sumRow = row + 1;
        worksheet.Cells[sumRow, 2].Value = "Total Confidence:";
        worksheet.Cells[sumRow, 3].Formula = $"SUM(C4:C{row - 1})";

        // Conditional formatting for validation (use dynamic expected sum)
        var validationCell = worksheet.Cells[sumRow, 3];
        var cf = worksheet.ConditionalFormatting.AddEqual(validationCell);
        cf.Formula = userPicks.ExpectedConfidenceSum.ToString();
        cf.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        cf.Style.Fill.BackgroundColor.Color = System.Drawing.Color.Green;

        // Auto-fit columns
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        return await Task.FromResult(package.GetAsByteArray());
    }
}
