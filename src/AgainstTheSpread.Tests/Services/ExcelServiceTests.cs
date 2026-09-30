using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;
using OfficeOpenXml;

namespace AgainstTheSpread.Tests.Services;

public class ExcelServiceTests : IDisposable
{
    private readonly ExcelService _excelService;

    public ExcelServiceTests()
    {
        _excelService = new ExcelService();
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithValidFile_ParsesCorrectly()
    {
        // Arrange
        var testData = CreateWeek1LinesExcel();
        using var stream = new MemoryStream(testData);

        // Act
        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Week.Should().Be(1);
        result.Games.Should().NotBeEmpty();
        result.Games.Should().HaveCount(4); // Test file has 4 games
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_ParsesGameDetails_Correctly()
    {
        // Arrange
        var testData = CreateWeek1LinesExcel();
        using var stream = new MemoryStream(testData);

        // Act
        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        // Assert - Check first game (Boise State @ South Florida)
        var firstGame = result.Games.FirstOrDefault(g => g.Favorite == "Boise State");
        firstGame.Should().NotBeNull();
        firstGame!.Line.Should().Be(-10m);
        firstGame.VsAt.Should().Be("at");
        firstGame.Underdog.Should().Be("South Florida");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithEmptyFile_ThrowsFormatException()
    {
        // Arrange
        var emptyExcel = CreateEmptyExcel();
        using var stream = new MemoryStream(emptyExcel);

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(
            async () => await _excelService.ParseWeeklyLinesAsync(stream));
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithoutWeekNumber_ThrowsFormatException()
    {
        // Arrange
        var excelWithoutWeek = CreateExcelWithoutWeekNumber();
        using var stream = new MemoryStream(excelWithoutWeek);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FormatException>(
            async () => await _excelService.ParseWeeklyLinesAsync(stream));
        exception.Message.Should().Contain("Could not find week number");
    }

    [Fact]
    public async Task GeneratePicksExcelAsync_WithValidPicks_GeneratesCorrectFormat()
    {
        // Arrange
        var userPicks = new UserPicks
        {
            Name = "Gary Harris",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Notre Dame", "Akron", "Michigan", "Alabama", "Clemson", "FSU" },
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var result = await _excelService.GeneratePicksExcelAsync(userPicks);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        // Verify the content
        using var package = new ExcelPackage(new MemoryStream(result));
        var worksheet = package.Workbook.Worksheets[0];

        // Row 1 and 2 should be empty
        worksheet.Cells[1, 1].Text.Should().BeEmpty();
        worksheet.Cells[2, 1].Text.Should().BeEmpty();

        // Row 3 should have headers
        worksheet.Cells[3, 1].Text.Should().Be("Name");
        worksheet.Cells[3, 2].Text.Should().Be("Pick 1");
        worksheet.Cells[3, 7].Text.Should().Be("Pick 6");

        // Row 4 should have user data
        worksheet.Cells[4, 1].Text.Should().Be("Gary Harris");
        worksheet.Cells[4, 2].Text.Should().Be("Notre Dame");
        worksheet.Cells[4, 7].Text.Should().Be("FSU");
    }

    [Fact]
    public async Task GeneratePicksExcelAsync_WithInvalidPicks_ThrowsArgumentException()
    {
        // Arrange
        var invalidPicks = new UserPicks
        {
            Name = "Test User",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2" } // Only 2 picks, needs 6
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _excelService.GeneratePicksExcelAsync(invalidPicks));
    }

    [Fact]
    public async Task GeneratePicksExcelAsync_CreatesExcelWithPicksSheet()
    {
        // Arrange
        var userPicks = new UserPicks
        {
            Name = "Test User",
            Week = 1,
            Year = 2024,
            Picks = new List<string> { "Team1", "Team2", "Team3", "Team4", "Team5", "Team6" },
            SubmittedAt = DateTime.UtcNow
        };

        // Act
        var result = await _excelService.GeneratePicksExcelAsync(userPicks);

        // Assert
        using var package = new ExcelPackage(new MemoryStream(result));
        package.Workbook.Worksheets.Should().HaveCount(1);
        package.Workbook.Worksheets[0].Name.Should().Be("Picks");
    }

    [Theory]
    [InlineData("Alabama", -9.5)]
    [InlineData("Ohio State", -3.5)]
    [InlineData("Clemson", -2.5)]
    public async Task ParseWeeklyLinesAsync_ParsesSpecificGames_Correctly(string favorite, double expectedLine)
    {
        // Arrange
        var testData = CreateWeek1LinesExcel();
        using var stream = new MemoryStream(testData);

        // Act
        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        // Assert
        var game = result.Games.FirstOrDefault(g => g.Favorite == favorite);
        game.Should().NotBeNull();
        game!.Line.Should().Be((decimal)expectedLine);
    }

    [Theory]
    [InlineData("Friday, September 19, 2026", 3)]
    [InlineData("Saturday, September 20, 2026", 3)]
    [InlineData("Friday, September 19, 2026", 4)]
    [InlineData("Saturday, September 20, 2026", 4)]
    public async Task ParseWeeklyLinesAsync_WithMismatchedDateHeader_RejectsInsteadOfReusingThursday(string header, int dateColumn)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Week 3 Lines");
        sheet.Cells[5, 4].Value = "Favorite";
        sheet.Cells[5, 5].Value = "Line";
        sheet.Cells[5, 6].Value = "vs/at";
        sheet.Cells[5, 7].Value = "Under Dog";
        sheet.Cells[7, dateColumn].Value = "Thursday, September 17, 2026";
        sheet.Cells[9, 4].Value = "Pittsburgh";
        sheet.Cells[9, 5].Value = -9.5;
        sheet.Cells[9, 7].Value = "Syracuse";
        sheet.Cells[11, dateColumn].Value = header;
        sheet.Cells[13, 4].Value = "Miami (FL)";
        sheet.Cells[13, 5].Value = -23.5;
        sheet.Cells[13, 7].Value = "Wake Forest";
        using var stream = new MemoryStream(package.GetAsByteArray());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => _excelService.ParseWeeklyLinesAsync(stream, 3, 2026));

        exception.Message.Should().Contain(sheet.Cells[11, dateColumn].Address).And.Contain(header)
            .And.Contain("weekday");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithMissingDate_RejectsInsteadOfUsingUploadTime()
    {
        using var package = new ExcelPackage(new MemoryStream(CreateWeek1LinesExcel()));
        package.Workbook.Worksheets[0].Cells[7, 1].Value = null;
        using var stream = new MemoryStream(package.GetAsByteArray());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => _excelService.ParseWeeklyLinesAsync(stream));

        exception.Message.Should().Contain("row 9").And.Contain("date header");
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public async Task ParseWeeklyLinesAsync_WithValidSectionDates_PreservesDistinctDays(int dateColumn)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Week 3 Lines");
        sheet.Cells[5, 4].Value = "Favorite";
        sheet.Cells[5, 5].Value = "Line";
        sheet.Cells[5, 6].Value = "vs/at";
        sheet.Cells[5, 7].Value = "Under Dog";
        var headers = new[] { "Thursday, September 17, 2026", "Friday, September 18, 2026", "Saturday, September 19, 2026" };
        for (int i = 0; i < headers.Length; i++)
        {
            int row = 7 + i * 4;
            sheet.Cells[row, dateColumn].Value = headers[i];
            sheet.Cells[row + 2, 4].Value = $"Favorite {i}";
            sheet.Cells[row + 2, 5].Value = -3.5;
            sheet.Cells[row + 2, 7].Value = $"Underdog {i}";
        }
        using var stream = new MemoryStream(package.GetAsByteArray());

        var result = await _excelService.ParseWeeklyLinesAsync(stream, 3, 2026);

        result.Games.Select(g => g.GameDate).Should().Equal(
            new DateTime(2026, 9, 17), new DateTime(2026, 9, 18), new DateTime(2026, 9, 19));
        result.Games.Select(g => g.Favorite).Should().Equal("Favorite 0", "Favorite 1", "Favorite 2");
        result.Games.Should().OnlyContain(g => g.Line == -3.5m);
    }

    [Theory]
    [InlineData("09.31.2026", 1)]
    [InlineData("09.31.2026", 2)]
    [InlineData("09.31.2026", 3)]
    [InlineData("09.31.2026", 4)]
    [InlineData("TBD", 1)]
    [InlineData("TBD", 2)]
    [InlineData("TBD", 3)]
    [InlineData("TBD", 4)]
    public async Task ParseWeeklyLinesAsync_WithUnparseableSectionHeader_RejectsInsteadOfReusingThursday(string header, int dateColumn)
    {
        using var stream = new MemoryStream(CreateSectionHeaderExcel(header, dateColumn));

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => _excelService.ParseWeeklyLinesAsync(stream, 3, 2026));

        exception.Message.Should().Contain("Invalid date header")
            .And.Contain(ExcelCellBase.GetAddress(11, dateColumn)).And.Contain(header);
    }

    [Theory]
    [InlineData("09.18.2026", 1, 18)]
    [InlineData("09.18.2026", 2, 18)]
    [InlineData("09.18.2026", 3, 18)]
    [InlineData("09.18.2026", 4, 18)]
    [InlineData(null, 3, 17)]
    [InlineData(null, 4, 17)]
    [InlineData("", 3, 17)]
    [InlineData("", 4, 17)]
    [InlineData("   ", 3, 17)]
    [InlineData("   ", 4, 17)]
    public async Task ParseWeeklyLinesAsync_WithDottedOrBlankSectionHeader_PreservesDatesAndTeams(string? header, int dateColumn, int secondGameDay)
    {
        using var stream = new MemoryStream(CreateSectionHeaderExcel(header, dateColumn));

        var result = await _excelService.ParseWeeklyLinesAsync(stream, 3, 2026);

        result.Games.Select(g => g.GameDate).Should().Equal(
            new DateTime(2026, 9, 17), new DateTime(2026, 9, secondGameDay));
        result.Games.Select(g => g.Favorite).Should().Equal("Pittsburgh", "Miami (FL)");
        result.Games.Select(g => g.Underdog).Should().Equal("Syracuse", "Wake Forest");
        result.Games.Select(g => g.Line).Should().Equal(-9.5m, -23.5m);
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithPartialGameRow_SkipsRowInsteadOfRejectingUpload()
    {
        using var package = new ExcelPackage(new MemoryStream(CreateWeek1LinesExcel()));
        var sheet = package.Workbook.Worksheets[0];
        // Admin has typed the team name but hasn't entered the line/underdog yet.
        sheet.Cells[25, 2].Value = "Pittsburgh";
        using var stream = new MemoryStream(package.GetAsByteArray());

        var result = await _excelService.ParseWeeklyLinesAsync(stream);

        result.Games.Should().HaveCount(4);
        result.Games.Should().NotContain(g => g.Favorite == "Pittsburgh");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithPartialGameRowInDedicatedDateColumnLayout_SkipsRowInsteadOfRejectingUpload()
    {
        using var package = new ExcelPackage(new MemoryStream(CreateSectionHeaderExcel("Friday, September 18, 2026", 3)));
        var sheet = package.Workbook.Worksheets[0];
        // Admin has typed the team name but hasn't entered the line/underdog yet.
        sheet.Cells[10, 4].Value = "Georgia";
        using var stream = new MemoryStream(package.GetAsByteArray());

        var result = await _excelService.ParseWeeklyLinesAsync(stream, 3, 2026);

        result.Games.Should().HaveCount(2);
        result.Games.Should().NotContain(g => g.Favorite == "Georgia");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithPartialGameRowInFavoriteColumnDateLayout_RejectsAmbiguousCandidate()
    {
        // No real reference workbook puts a date in the Favorite column itself - all four
        // Week N Lines.xlsx files use a dedicated date column left of Favorite. In this
        // synthetic layout a partial row ("Georgia", no line/underdog) is byte-for-byte
        // identical in shape to a genuinely invalid date header ("TBD"), so rejecting is the
        // deliberate, safer choice over silently skipping and risking a stale date on a real
        // game later in the file.
        using var package = new ExcelPackage(new MemoryStream(CreateSectionHeaderExcel("Friday, September 18, 2026", 4)));
        var sheet = package.Workbook.Worksheets[0];
        sheet.Cells[10, 4].Value = "Georgia";
        using var stream = new MemoryStream(package.GetAsByteArray());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => _excelService.ParseWeeklyLinesAsync(stream, 3, 2026));

        exception.Message.Should().Contain(ExcelCellBase.GetAddress(10, 4)).And.Contain("Georgia");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithUnresolvedFavoriteColumnHeaderAndNoValidDateEver_NamesTheOffendingCell()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Week 3 Lines");
        sheet.Cells[5, 4].Value = "Favorite";
        sheet.Cells[5, 5].Value = "Line";
        sheet.Cells[5, 6].Value = "vs/at";
        sheet.Cells[5, 7].Value = "Under Dog";
        sheet.Cells[7, 4].Value = "TBD";
        sheet.Cells[9, 4].Value = "Pittsburgh";
        sheet.Cells[9, 5].Value = -9.5;
        sheet.Cells[9, 7].Value = "Syracuse";
        using var stream = new MemoryStream(package.GetAsByteArray());

        var exception = await Assert.ThrowsAsync<FormatException>(
            () => _excelService.ParseWeeklyLinesAsync(stream, 3, 2026));

        exception.Message.Should().Contain("row 9").And.Contain("date header")
            .And.Contain(sheet.Cells[7, 4].Address).And.Contain("TBD");
    }

    [Fact]
    public async Task ParseWeeklyLinesAsync_WithMultipleParseableDatesInHeaderRow_UsesFirstMatch()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Week 3 Lines");
        sheet.Cells[5, 4].Value = "Favorite";
        sheet.Cells[5, 5].Value = "Line";
        sheet.Cells[5, 6].Value = "vs/at";
        sheet.Cells[5, 7].Value = "Under Dog";
        sheet.Cells[7, 1].Value = "Thursday, September 17, 2026";
        sheet.Cells[7, 3].Value = "Friday, September 18, 2026";
        sheet.Cells[9, 4].Value = "Pittsburgh";
        sheet.Cells[9, 5].Value = -9.5;
        sheet.Cells[9, 7].Value = "Syracuse";
        using var stream = new MemoryStream(package.GetAsByteArray());

        var result = await _excelService.ParseWeeklyLinesAsync(stream, 3, 2026);

        result.Games.Should().ContainSingle().Which.GameDate.Should().Be(new DateTime(2026, 9, 17));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(11)]
    [InlineData(12)]
    public async Task ParseWeeklyLinesAsync_WithValidReferenceWorkbook_AcceptsGames(int week)
    {
        var filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../reference-docs", $"Week {week} Lines.xlsx"));
        using var stream = File.OpenRead(filePath);

        var result = await _excelService.ParseWeeklyLinesAsync(stream, week, 2025);

        result.Games.Should().NotBeEmpty();
        result.Games.Should().OnlyContain(g => g.GameDate.Year == 2025);
    }

    // Helper methods to create test Excel files

    private byte[] CreateSectionHeaderExcel(string? header, int dateColumn)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Week 3 Lines");
        sheet.Cells[5, 4].Value = "Favorite";
        sheet.Cells[5, 5].Value = "Line";
        sheet.Cells[5, 6].Value = "vs/at";
        sheet.Cells[5, 7].Value = "Under Dog";
        sheet.Cells[7, dateColumn].Value = "Thursday, September 17, 2026";
        sheet.Cells[9, 4].Value = "Pittsburgh";
        sheet.Cells[9, 5].Value = -9.5;
        sheet.Cells[9, 7].Value = "Syracuse";
        sheet.Cells[11, dateColumn].Value = header;
        sheet.Cells[13, 4].Value = "Miami (FL)";
        sheet.Cells[13, 5].Value = -23.5;
        sheet.Cells[13, 7].Value = "Wake Forest";
        return package.GetAsByteArray();
    }

    private byte[] CreateWeek1LinesExcel()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Lines");

        // Empty rows
        worksheet.Cells[1, 1].Value = string.Empty;
        worksheet.Cells[2, 1].Value = string.Empty;

        // Week header
        worksheet.Cells[3, 1].Value = "WEEK 1";
        worksheet.Cells[4, 1].Value = string.Empty;

        // Column headers
        worksheet.Cells[5, 2].Value = "Favorite";
        worksheet.Cells[5, 3].Value = "Line";
        worksheet.Cells[5, 4].Value = "vs/at";
        worksheet.Cells[5, 5].Value = "Under Dog";
        worksheet.Cells[6, 1].Value = string.Empty;

        // Date header
        worksheet.Cells[7, 1].Value = "Thursday, August 28, 2025";
        worksheet.Cells[8, 1].Value = string.Empty;

        // Games
        int row = 9;
        worksheet.Cells[row, 2].Value = "Boise State";
        worksheet.Cells[row, 3].Value = -10;
        worksheet.Cells[row, 4].Value = "at";
        worksheet.Cells[row, 5].Value = "South Florida";

        row++;
        worksheet.Cells[row, 2].Value = "Alabama";
        worksheet.Cells[row, 3].Value = -9.5;
        worksheet.Cells[row, 4].Value = "vs";
        worksheet.Cells[row, 5].Value = "Florida State";

        row++;
        worksheet.Cells[row, 2].Value = "Ohio State";
        worksheet.Cells[row, 3].Value = -3.5;
        worksheet.Cells[row, 4].Value = "vs";
        worksheet.Cells[row, 5].Value = "Texas";

        row++;
        worksheet.Cells[row, 2].Value = "Clemson";
        worksheet.Cells[row, 3].Value = -2.5;
        worksheet.Cells[row, 4].Value = "at";
        worksheet.Cells[row, 5].Value = "LSU";

        return package.GetAsByteArray();
    }

    private byte[] CreateEmptyExcel()
    {
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Sheet1");
        return package.GetAsByteArray();
    }

    private byte[] CreateExcelWithoutWeekNumber()
    {
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Lines");

        // Headers without week
        worksheet.Cells[1, 2].Value = "Favorite";
        worksheet.Cells[1, 3].Value = "Line";
        worksheet.Cells[1, 4].Value = "vs/at";
        worksheet.Cells[1, 5].Value = "Under Dog";

        // A game
        worksheet.Cells[2, 2].Value = "Team A";
        worksheet.Cells[2, 3].Value = -7;
        worksheet.Cells[2, 4].Value = "vs";
        worksheet.Cells[2, 5].Value = "Team B";

        return package.GetAsByteArray();
    }
}
