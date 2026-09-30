# Bowl Games Implementation Plan

This document provides a detailed implementation plan for adding bowl game support to the Against The Spread application. The bowl games feature is completely separate from regular season functionality to prevent any risk of breaking existing features.

## Table of Contents

1. [Overview](#overview)
2. [Key Differences from Regular Season](#key-differences-from-regular-season)
3. [Phase 1: Core Models](#phase-1-core-models)
4. [Phase 2: Excel Services](#phase-2-excel-services)
5. [Phase 3: API Functions](#phase-3-api-functions)
6. [Phase 4: Web UI](#phase-4-web-ui)
7. [Phase 5: Testing](#phase-5-testing)
8. [Phase 6: Integration and E2E Testing](#phase-6-integration-and-e2e-testing)
9. [File Structure](#file-structure)
10. [Excel Format Specifications](#excel-format-specifications)
11. [Testing Checkpoints](#testing-checkpoints)

---

## Overview

Bowl games require a completely different pick mechanism compared to the regular season:

### Regular Season
- Pick 6 games against the spread each week
- Win all 6 to qualify for weekly payout
- Simple pick format: team name only

### Bowl Games
- Pick ALL bowl games (typically 36-43 games depending on the season)
- Four data points per game:
  1. **Winner Against the Spread** - Which team covers the spread
  2. **Confidence Points** - Unique value 1-N for each game (N = most confident)
  3. **Outright Winner** - Which team wins regardless of spread
- Two separate payouts:
  - Most total confidence points earned
  - Most outright game wins

### Validation Rules
- Each game must have unique confidence points (1 through total game count)
- Sum of all confidence points must equal N(N+1)/2 where N = total games
- **Note**: The original requirements mention 36 games with sum = 703, but mathematically 1+2+...+36 = 666. A sum of 703 = 1+2+...+37 (37 games). The implementation should be flexible and calculate the expected sum dynamically based on the actual number of games in the uploaded lines file.
- Team names must match exactly as they appear on the Lines sheet
- Cannot change the order of games on the template

---

## Key Differences from Regular Season

| Aspect | Regular Season | Bowl Games |
|--------|---------------|------------|
| Number of games | 6 per week | All bowl games (typically 36-43) |
| Pick type | Team against spread | Spread pick + confidence + outright winner |
| Confidence points | N/A | 1-N unique per game (N = game count) |
| Payouts | 1 (all picks correct) | 2 (confidence points + outright wins) |
| Frequency | Weekly | Once per season |
| Week identifier | 1-14 | "bowls" or special identifier |

---

## Phase 1: Core Models

### Checklist
- [x] Create `BowlGame.cs` model
- [x] Create `BowlPick.cs` model (single game pick with confidence)
- [x] Create `BowlUserPicks.cs` model (all picks for a user)
- [x] Create `BowlLines.cs` model (all bowl game lines)
- [x] Add unit tests for all models
- [x] Verify build passes

### 1.1 BowlGame Model

**File:** `src/AgainstTheSpread.Core/Models/BowlGame.cs`

```csharp
namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a single bowl game with betting line information.
/// Bowl games differ from regular season games by having a bowl name.
/// </summary>
public class BowlGame
{
    /// <summary>
    /// Name of the bowl game (e.g., "Rose Bowl", "Sugar Bowl")
    /// </summary>
    public string BowlName { get; set; } = string.Empty;

    /// <summary>
    /// Game sequence number (1-36, determines order on template)
    /// </summary>
    public int GameNumber { get; set; }

    /// <summary>
    /// The favored team name
    /// </summary>
    public string Favorite { get; set; } = string.Empty;

    /// <summary>
    /// The point spread (negative number indicating favorite margin)
    /// </summary>
    public decimal Line { get; set; }

    /// <summary>
    /// The underdog team name
    /// </summary>
    public string Underdog { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the game is scheduled
    /// </summary>
    public DateTime GameDate { get; set; }

    /// <summary>
    /// Display string for the favorite with line (e.g., "Alabama -9.5")
    /// </summary>
    public string FavoriteDisplay => $"{Favorite} {Line}";

    /// <summary>
    /// Full game description
    /// </summary>
    public string GameDescription => $"{BowlName}: {Favorite} {Line} vs {Underdog}";
}
```

### 1.2 BowlPick Model

**File:** `src/AgainstTheSpread.Core/Models/BowlPick.cs`

```csharp
namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a user's pick for a single bowl game.
/// Each bowl pick includes the spread pick, confidence points, and outright winner.
/// </summary>
public class BowlPick
{
    /// <summary>
    /// The game number this pick is for (1 through total game count)
    /// </summary>
    public int GameNumber { get; set; }

    /// <summary>
    /// The team picked against the spread
    /// </summary>
    public string SpreadPick { get; set; } = string.Empty;

    /// <summary>
    /// Confidence points assigned to this pick (1 through total game count, unique per game)
    /// Higher = more confident
    /// </summary>
    public int ConfidencePoints { get; set; }

    /// <summary>
    /// The team picked to win outright (regardless of spread)
    /// </summary>
    public string OutrightWinner { get; set; } = string.Empty;

    /// <summary>
    /// Validates that this pick is complete for a given total game count
    /// </summary>
    public bool IsValid(int totalGames = 36)
    {
        return GameNumber >= 1 
            && GameNumber <= totalGames
            && !string.IsNullOrWhiteSpace(SpreadPick)
            && ConfidencePoints >= 1 
            && ConfidencePoints <= totalGames
            && !string.IsNullOrWhiteSpace(OutrightWinner);
    }
}
```

### 1.3 BowlUserPicks Model

**File:** `src/AgainstTheSpread.Core/Models/BowlUserPicks.cs`

```csharp
namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents a user's complete bowl picks for all games.
/// Bowl picks require unique confidence points 1-N and sum to N(N+1)/2.
/// </summary>
public class BowlUserPicks
{
    /// <summary>
    /// User's name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Year of the bowl season
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total number of bowl games for this season (determined from lines file)
    /// </summary>
    public int TotalGames { get; set; }

    /// <summary>
    /// List of all bowl picks (must match TotalGames count)
    /// </summary>
    public List<BowlPick> Picks { get; set; } = new();

    /// <summary>
    /// When the picks were submitted
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Calculates expected confidence sum for validation: N(N+1)/2
    /// </summary>
    public int ExpectedConfidenceSum => TotalGames * (TotalGames + 1) / 2;

    /// <summary>
    /// Validates that all picks are complete and confidence points are valid
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        if (Year < 2020)
            return false;

        if (TotalGames < 1)
            return false;

        if (Picks.Count != TotalGames)
            return false;

        if (!Picks.All(p => p.IsValid(TotalGames)))
            return false;

        // Check unique confidence points
        var confidencePoints = Picks.Select(p => p.ConfidencePoints).ToList();
        if (confidencePoints.Distinct().Count() != TotalGames)
            return false;

        // Check sum equals N(N+1)/2
        if (confidencePoints.Sum() != ExpectedConfidenceSum)
            return false;

        return true;
    }

    /// <summary>
    /// Gets validation error message if invalid
    /// </summary>
    public string? GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return "Name is required";

        if (Year < 2020)
            return "Invalid year";

        if (TotalGames < 1)
            return "Total games must be set";

        if (Picks.Count != TotalGames)
            return $"Exactly {TotalGames} picks are required (you have {Picks.Count})";

        var invalidPicks = Picks.Where(p => !p.IsValid(TotalGames)).ToList();
        if (invalidPicks.Any())
            return $"Invalid picks found for games: {string.Join(", ", invalidPicks.Select(p => p.GameNumber))}";

        var confidencePoints = Picks.Select(p => p.ConfidencePoints).ToList();
        var duplicates = confidencePoints.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key);
        if (duplicates.Any())
            return $"Duplicate confidence points found: {string.Join(", ", duplicates)}";

        var sum = confidencePoints.Sum();
        if (sum != ExpectedConfidenceSum)
            return $"Confidence points must sum to {ExpectedConfidenceSum} (yours: {sum})";

        return null;
    }

    /// <summary>
    /// Calculates if confidence points sum to the expected value
    /// </summary>
    public bool HasValidConfidenceSum()
    {
        return Picks.Sum(p => p.ConfidencePoints) == ExpectedConfidenceSum;
    }
}
```

### 1.4 BowlLines Model

**File:** `src/AgainstTheSpread.Core/Models/BowlLines.cs`

```csharp
namespace AgainstTheSpread.Core.Models;

/// <summary>
/// Represents all bowl game lines for a season.
/// </summary>
public class BowlLines
{
    /// <summary>
    /// Year of the bowl season
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// List of all bowl games
    /// </summary>
    public List<BowlGame> Games { get; set; } = new();

    /// <summary>
    /// When the lines were uploaded by admin
    /// </summary>
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Total number of games
    /// </summary>
    public int TotalGames => Games.Count;

    /// <summary>
    /// Validates that the bowl lines data is complete
    /// </summary>
    public bool IsValid() => Games != null && Games.Any() && Year >= 2020;
}
```

### Testing Checkpoint 1
```bash
# After creating models, run:
dotnet build
dotnet test --filter "Category=BowlModels"
```

---

## Phase 2: Excel Services

### Checklist
- [x] Create `IBowlExcelService.cs` interface
- [x] Create `BowlExcelService.cs` implementation
- [x] Implement `ParseBowlLinesAsync` method
- [x] Implement `GenerateBowlPicksExcelAsync` method
- [x] Add unit tests for bowl Excel parsing
- [x] Add unit tests for bowl Excel generation
- [x] Verify build passes

### 2.1 Interface

**File:** `src/AgainstTheSpread.Core/Interfaces/IBowlExcelService.cs`

```csharp
using AgainstTheSpread.Core.Models;

namespace AgainstTheSpread.Core.Interfaces;

/// <summary>
/// Service for parsing and generating Excel files for bowl games.
/// Separate from regular season to avoid breaking existing functionality.
/// </summary>
public interface IBowlExcelService
{
    /// <summary>
    /// Parses the bowl lines Excel file uploaded by admin
    /// </summary>
    Task<BowlLines> ParseBowlLinesAsync(Stream excelStream, int? year = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an Excel file with user's bowl picks in the required format
    /// </summary>
    Task<byte[]> GenerateBowlPicksExcelAsync(BowlUserPicks userPicks, CancellationToken cancellationToken = default);
}
```

### 2.2 Implementation

**File:** `src/AgainstTheSpread.Core/Services/BowlExcelService.cs`

```csharp
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
                if (cellValue?.Contains("Bowl", StringComparison.OrdinalIgnoreCase) == true &&
                    cellValue?.Contains("Name", StringComparison.OrdinalIgnoreCase) == true)
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
        for (int row = headerRow + 1; row <= worksheet.Dimension?.End.Row; row++)
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
```

### Testing Checkpoint 2
```bash
# After creating Excel services, run:
dotnet build
dotnet test --filter "Category=BowlExcel"
```

---

## Phase 3: API Functions

### Checklist
- [x] Create `BowlLinesFunction.cs` for getting bowl lines
- [x] Create `BowlPicksFunction.cs` for submitting bowl picks
- [x] Create `UploadBowlLinesFunction.cs` for admin upload
- [x] Register bowl services in DI container (Program.cs)
- [x] Add unit tests for all functions
- [x] Verify build passes

### 3.1 Bowl Lines Function

**File:** `src/AgainstTheSpread.Functions/BowlLinesFunction.cs`

```csharp
using AgainstTheSpread.Core.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for retrieving bowl game lines.
/// Separate from regular season lines endpoint.
/// </summary>
public class BowlLinesFunction
{
    private readonly ILogger<BowlLinesFunction> _logger;
    private readonly IStorageService _storageService;

    public BowlLinesFunction(ILogger<BowlLinesFunction> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
    }

    /// <summary>
    /// GET /api/bowl-lines?year={year}
    /// Returns all bowl games with lines for a specific year
    /// </summary>
    [Function("GetBowlLines")]
    public async Task<HttpResponseData> GetBowlLines(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "bowl-lines")] HttpRequestData req)
    {
        _logger.LogInformation("Processing GetBowlLines request");

        try
        {
            var yearString = req.Query["year"];
            var year = string.IsNullOrEmpty(yearString)
                ? DateTime.UtcNow.Year
                : int.Parse(yearString!);

            var lines = await _storageService.GetBowlLinesAsync(year);

            if (lines == null)
            {
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                await notFoundResponse.WriteAsJsonAsync(new
                {
                    error = $"Bowl lines not found for {year}"
                });
                return notFoundResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(lines);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bowl lines");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to retrieve bowl lines" });
            return errorResponse;
        }
    }
}
```

### 3.2 Bowl Picks Function

**File:** `src/AgainstTheSpread.Functions/BowlPicksFunction.cs`

```csharp
using AgainstTheSpread.Core.Interfaces;
using AgainstTheSpread.Core.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AgainstTheSpread.Functions;

/// <summary>
/// API endpoint for submitting bowl picks and downloading Excel.
/// </summary>
public class BowlPicksFunction
{
    private readonly ILogger<BowlPicksFunction> _logger;
    private readonly IBowlExcelService _bowlExcelService;

    public BowlPicksFunction(ILogger<BowlPicksFunction> logger, IBowlExcelService bowlExcelService)
    {
        _logger = logger;
        _bowlExcelService = bowlExcelService;
    }

    /// <summary>
    /// POST /api/bowl-picks
    /// Accepts bowl picks and returns Excel file in required format
    /// </summary>
    [Function("SubmitBowlPicks")]
    public async Task<HttpResponseData> SubmitBowlPicks(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bowl-picks")] HttpRequestData req)
    {
        _logger.LogInformation("Processing SubmitBowlPicks request");

        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var userPicks = JsonSerializer.Deserialize<BowlUserPicks>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userPicks == null)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteAsJsonAsync(new { error = "Invalid request body" });
                return badResponse;
            }

            userPicks.SubmittedAt = DateTime.UtcNow;

            if (!userPicks.IsValid())
            {
                var validationError = userPicks.GetValidationError();
                var validationResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await validationResponse.WriteAsJsonAsync(new { error = validationError });
                return validationResponse;
            }

            var excelBytes = await _bowlExcelService.GenerateBowlPicksExcelAsync(userPicks);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            response.Headers.Add("Content-Disposition",
                $"attachment; filename=\"{userPicks.Name.Replace(" ", "_")}_Bowl_Picks_{userPicks.Year}.xlsx\"");
            await response.Body.WriteAsync(excelBytes);
            return response;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in SubmitBowlPicks");
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteAsJsonAsync(new { error = ex.Message });
            return badResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bowl picks submission");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new { error = "Failed to generate bowl picks file" });
            return errorResponse;
        }
    }
}
```

### 3.3 Update Storage Service Interface

**Add to:** `src/AgainstTheSpread.Core/Interfaces/IStorageService.cs`

```csharp
// Add these methods to the existing interface:

/// <summary>
/// Gets bowl lines for a specific year
/// </summary>
Task<BowlLines?> GetBowlLinesAsync(int year, CancellationToken cancellationToken = default);

/// <summary>
/// Uploads bowl lines for a specific year
/// </summary>
Task UploadBowlLinesAsync(Stream excelStream, int year, CancellationToken cancellationToken = default);
```

### 3.4 Update Program.cs for DI

**Add to:** `src/AgainstTheSpread.Functions/Program.cs`

```csharp
// Add this line with the other service registrations:
services.AddSingleton<IBowlExcelService, BowlExcelService>();
```

### Testing Checkpoint 3
```bash
# After creating functions, run:
dotnet build
dotnet test --filter "Category=BowlFunctions"
```

---

## Phase 4: Web UI

### Checklist
- [x] Create `BowlPicks.razor` page component
- [x] Add bowl-specific CSS styles
- [x] Add confidence points validation UI
- [x] Add bowl game card components
- [x] Add navigation link to bowl picks
- [x] Update API service for bowl endpoints
- [x] Verify build passes

### 4.1 Bowl Picks Page

**File:** `src/AgainstTheSpread.Web/Pages/BowlPicks.razor`

This will be a new page specifically for bowl picks at `/bowl-picks`.

Key UI features:
- List all bowl games in order (DO NOT change order per requirements)
- For each game:
  - Display bowl name, matchup, and spread
  - Dropdown/buttons for spread pick (Favorite or Underdog)
  - Dropdown for confidence points (1-N, show which are used)
  - Dropdown/buttons for outright winner
- Real-time validation:
  - Show confidence points sum (must = N(N+1)/2 where N = game count)
  - Highlight duplicate confidence points
  - Show green when sum is valid, red otherwise
- Generate button (enabled only when valid)
- Print button for physical sheet

### 4.2 API Service Updates

**Add to:** `src/AgainstTheSpread.Web/Services/ApiService.cs`

```csharp
// Add these methods:

public async Task<BowlLines?> GetBowlLinesAsync(int year)
{
    try
    {
        return await _http.GetFromJsonAsync<BowlLines>($"/api/bowl-lines?year={year}");
    }
    catch
    {
        return null;
    }
}

public async Task<byte[]?> SubmitBowlPicksAsync(BowlUserPicks picks)
{
    try
    {
        var response = await _http.PostAsJsonAsync("/api/bowl-picks", picks);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        return null;
    }
    catch
    {
        return null;
    }
}
```

### 4.3 Navigation Update

**Update:** `src/AgainstTheSpread.Web/Layout/NavMenu.razor`

Add a link to the bowl picks page (only show during bowl season or with admin flag).

### Testing Checkpoint 4
```bash
# After creating UI components, run:
dotnet build
dotnet test --filter "Category=BowlWeb"
```

---

## Phase 5: Testing

### Unit Tests Checklist
- [ ] BowlGame model tests
- [ ] BowlPick model tests
- [ ] BowlUserPicks model tests (validation, confidence sum)
- [ ] BowlLines model tests
- [ ] BowlExcelService parsing tests
- [ ] BowlExcelService generation tests
- [ ] BowlLinesFunction tests
- [ ] BowlPicksFunction tests

### Test Categories

Create test files in `src/AgainstTheSpread.Tests/`:

```
Models/
  BowlGameTests.cs
  BowlPickTests.cs
  BowlUserPicksTests.cs
  BowlLinesTests.cs

Services/
  BowlExcelServiceTests.cs

Functions/
  BowlLinesFunctionTests.cs
  BowlPicksFunctionTests.cs
```

### Key Test Scenarios

#### BowlUserPicks Validation Tests
1. Valid picks with correct confidence sum (N(N+1)/2)
2. Invalid: Duplicate confidence points
3. Invalid: Missing picks
4. Invalid: Confidence sum incorrect
5. Invalid: Empty name
6. Invalid: Invalid year

#### BowlExcelService Tests
1. Parse valid bowl lines file
2. Handle missing columns gracefully
3. Generate valid picks Excel with correct format
4. Validation cell shows dynamic expected sum

### Testing Checkpoint 5
```bash
# After creating all tests, run:
dotnet test
# Expected: All tests pass
```

---

## Phase 6: Integration and E2E Testing

### E2E Tests Checklist
- [ ] Add bowl lines upload E2E test
- [ ] Add bowl picks submission E2E test
- [ ] Add confidence validation E2E test
- [ ] Add Excel download verification E2E test

### E2E Test File

**File:** `tests/specs/bowl-flow.spec.ts`

Key test scenarios:
1. Admin uploads bowl lines file
2. User loads bowl picks page
3. User fills in all picks with valid confidence
4. System validates confidence sum dynamically
5. User downloads Excel picks
6. Verify Excel format matches template

### Testing Checkpoint 6
```bash
# Run E2E tests:
./start-e2e.sh
cd tests && npm test
./stop-e2e.sh
```

---

## File Structure

After implementation, new files will be:

```
src/
├── AgainstTheSpread.Core/
│   ├── Interfaces/
│   │   └── IBowlExcelService.cs          # NEW
│   ├── Models/
│   │   ├── BowlGame.cs                   # NEW
│   │   ├── BowlPick.cs                   # NEW
│   │   ├── BowlUserPicks.cs              # NEW
│   │   └── BowlLines.cs                  # NEW
│   └── Services/
│       └── BowlExcelService.cs           # NEW
├── AgainstTheSpread.Functions/
│   ├── BowlLinesFunction.cs              # NEW
│   ├── BowlPicksFunction.cs              # NEW
│   └── UploadBowlLinesFunction.cs        # NEW
├── AgainstTheSpread.Web/
│   ├── Pages/
│   │   └── BowlPicks.razor               # NEW
│   └── Services/
│       └── ApiService.cs                 # MODIFY (add bowl methods)
└── AgainstTheSpread.Tests/
    ├── Models/
    │   ├── BowlGameTests.cs              # NEW
    │   ├── BowlPickTests.cs              # NEW
    │   ├── BowlUserPicksTests.cs         # NEW
    │   └── BowlLinesTests.cs             # NEW
    ├── Services/
    │   └── BowlExcelServiceTests.cs      # NEW
    └── Functions/
        ├── BowlLinesFunctionTests.cs     # NEW
        └── BowlPicksFunctionTests.cs     # NEW

tests/
└── specs/
    └── bowl-flow.spec.ts                 # NEW
```

---

## Excel Format Specifications

### Bowl Lines Input Format (Admin Upload)

Based on "Bowl Lines" attachment, expected columns:
| Column | Description |
|--------|-------------|
| Bowl Name | Name of the bowl game |
| Date | Game date/time |
| Favorite | Favored team name |
| Line | Point spread (e.g., -7.5) |
| Underdog | Underdog team name |

### Bowl Template Output Format (User Picks)

Based on "Bowl Template" description:
| Cell | Content |
|------|---------|
| Yellow cell | User's name |
| Column A | Game number (1-36, fixed order) |
| Column B | Winner against the spread (team name) |
| Column C | Confidence points (1-36, unique) |
| Column D | Outright winner (team name) |
| H43 | Sum formula - shows 703 when valid, green/red conditional |

### Validation Rules in Excel
- Cell H43 shows sum of confidence points
- If sum = 703: Cell turns GREEN
- If sum ≠ 703: Cell turns RED
- All team names must match Lines sheet exactly

---

## Testing Checkpoints

Use these checkpoints after completing each phase:

### Checkpoint 1: Models Complete
```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Bowl" --filter "Category!=Integration"
```
Expected: All bowl model tests pass.

### Checkpoint 2: Excel Services Complete
```bash
dotnet build
dotnet test --filter "FullyQualifiedName~BowlExcel"
```
Expected: All bowl Excel tests pass.

### Checkpoint 3: Functions Complete
```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Bowl"
```
Expected: All bowl-related tests pass.

### Checkpoint 4: Web UI Complete
```bash
dotnet build
dotnet publish src/AgainstTheSpread.Web -c Debug
```
Expected: Web app builds and publishes successfully.

### Checkpoint 5: Unit Tests Complete
```bash
dotnet test
```
Expected: All 175+ existing tests + new bowl tests pass.

### Checkpoint 6: E2E Tests Complete
```bash
./start-e2e.sh
cd tests && npm test
./stop-e2e.sh
```
Expected: All E2E tests pass, including new bowl flow.

---

## Risk Mitigation

### Preventing Regular Season Breakage

1. **Separate Models**: All bowl models are new files, no modifications to existing models
2. **Separate Services**: IBowlExcelService is a new interface, IExcelService unchanged
3. **Separate Endpoints**: /api/bowl-lines and /api/bowl-picks are new routes
4. **Separate Pages**: /bowl-picks is a new page, /picks unchanged
5. **Separate Tests**: Bowl tests are in new files, existing tests unchanged

### Rollback Strategy

If issues arise:
1. All bowl code is in new files that can be deleted
2. No modifications to existing regular season code
3. Navigation links can be removed without affecting core functionality

---

## Success Criteria

The implementation is complete when:

1. ✅ All existing tests continue to pass (175+)
2. ✅ New bowl model tests pass
3. ✅ New bowl Excel service tests pass
4. ✅ New bowl function tests pass
5. ✅ New bowl component tests pass
6. ✅ E2E tests pass including bowl flow
7. ✅ Admin can upload bowl lines
8. ✅ User can select spread picks for all games
9. ✅ User can assign unique confidence points 1-N
10. ✅ User can select outright winners for all games
11. ✅ System validates confidence sum dynamically (N(N+1)/2)
12. ✅ User can download Excel with correct format
13. ✅ Excel shows validation cell with expected sum

---

## Estimated Timeline

| Phase | Duration | Dependencies |
|-------|----------|--------------|
| Phase 1: Models | 1-2 hours | None |
| Phase 2: Excel Services | 2-3 hours | Phase 1 |
| Phase 3: API Functions | 2-3 hours | Phase 1, 2 |
| Phase 4: Web UI | 4-6 hours | Phase 1, 2, 3 |
| Phase 5: Unit Tests | 2-4 hours | Phase 1-4 |
| Phase 6: E2E Tests | 2-3 hours | Phase 1-5 |

**Total Estimated Time: 13-21 hours**

---

## Notes for LLM Agents

When implementing this plan:

1. **Follow the checklist items in order** - each phase builds on the previous
2. **Run tests after each checkpoint** - catch issues early
3. **Never modify existing files** except where explicitly noted (ApiService.cs, Program.cs)
4. **Use exact model names and properties** as specified
5. **Match Excel format exactly** as described in the specifications
6. **Keep all bowl code separate** from regular season code
7. **Test confidence validation thoroughly** - it's the most complex part

### Common Pitfalls to Avoid

- ❌ Modifying existing Game, UserPicks, or WeeklyLines models
- ❌ Changing existing IExcelService or ExcelService
- ❌ Modifying existing API endpoints
- ❌ Breaking the regular season Picks.razor page
- ❌ Using week numbers for bowl games (use year + "bowls" identifier)
- ❌ Allowing duplicate confidence points
- ❌ Changing game order in the template

### Key Validation Points

- Confidence points: 1-N, unique, sum = N(N+1)/2
- Team names: Must match exactly as uploaded
- Game order: Must remain fixed as uploaded
- All games: Must have picks before download
