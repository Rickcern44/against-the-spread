using AgainstTheSpread.Core.Models;
using AgainstTheSpread.Core.Services;
using AwesomeAssertions;
using OfficeOpenXml;
using Xunit;

namespace AgainstTheSpread.Tests.Services;

public class BowlExcelServiceTests
{
    private readonly BowlExcelService _service;

    public BowlExcelServiceTests()
    {
        _service = new BowlExcelService();
    }

    private Stream CreateBowlLinesExcel(List<(string BowlName, string Favorite, decimal Line, string Underdog)> games)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        var stream = new MemoryStream();
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Bowl Lines");

            // Headers in row 1
            worksheet.Cells[1, 1].Value = "Bowl Name";
            worksheet.Cells[1, 2].Value = "Favorite";
            worksheet.Cells[1, 3].Value = "Line";
            worksheet.Cells[1, 4].Value = "Under Dog";

            // Data rows
            int row = 2;
            foreach (var game in games)
            {
                worksheet.Cells[row, 1].Value = game.BowlName;
                worksheet.Cells[row, 2].Value = game.Favorite;
                worksheet.Cells[row, 3].Value = game.Line;
                worksheet.Cells[row, 4].Value = game.Underdog;
                row++;
            }

            package.SaveAs(stream);
        }
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task ParseBowlLinesAsync_ValidFile_ReturnsParsedLines()
    {
        // Arrange
        var games = new List<(string, string, decimal, string)>
        {
            ("Rose Bowl", "Oregon", -3.5m, "Ohio State"),
            ("Sugar Bowl", "Georgia", -6.5m, "Baylor"),
            ("Orange Bowl", "Florida State", -2.5m, "Oklahoma")
        };
        using var stream = CreateBowlLinesExcel(games);

        // Act
        var result = await _service.ParseBowlLinesAsync(stream, 2024);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(2024);
        result.Games.Should().HaveCount(3);
        result.Games[0].BowlName.Should().Be("Rose Bowl");
        result.Games[0].Favorite.Should().Be("Oregon");
        result.Games[0].Line.Should().Be(-3.5m);
        result.Games[0].Underdog.Should().Be("Ohio State");
        result.Games[0].GameNumber.Should().Be(1);
    }

    [Fact]
    public async Task ParseBowlLinesAsync_DefaultYear_UsesCurrentYear()
    {
        // Arrange
        var games = new List<(string, string, decimal, string)>
        {
            ("Rose Bowl", "Oregon", -3.5m, "Ohio State")
        };
        using var stream = CreateBowlLinesExcel(games);

        // Act
        var result = await _service.ParseBowlLinesAsync(stream);

        // Assert
        result.Year.Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public async Task ParseBowlLinesAsync_EmptyFile_ThrowsFormatException()
    {
        // Arrange
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Bowl Lines");
            worksheet.Cells[1, 1].Value = "Bowl Name";
            worksheet.Cells[1, 2].Value = "Favorite";
            worksheet.Cells[1, 3].Value = "Line";
            worksheet.Cells[1, 4].Value = "Under Dog";

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _service.ParseBowlLinesAsync(stream, 2024));
        }
    }

    [Fact]
    public async Task ParseBowlLinesAsync_MissingColumns_ThrowsFormatException()
    {
        // Arrange
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Bowl Lines");
            worksheet.Cells[1, 1].Value = "Only One Column";

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => 
                _service.ParseBowlLinesAsync(stream, 2024));
        }
    }

    private List<BowlPick> CreateValidPicks(int totalGames)
    {
        var picks = new List<BowlPick>();
        for (int i = 1; i <= totalGames; i++)
        {
            picks.Add(new BowlPick
            {
                GameNumber = i,
                SpreadPick = $"Team{i}",
                ConfidencePoints = i,
                OutrightWinner = $"Team{i}"
            });
        }
        return picks;
    }

    [Fact]
    public async Task GenerateBowlPicksExcelAsync_ValidPicks_ReturnsExcelBytes()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act
        var result = await _service.GenerateBowlPicksExcelAsync(userPicks);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateBowlPicksExcelAsync_ValidPicks_ContainsCorrectData()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 3,
            Picks = new List<BowlPick>
            {
                new BowlPick { GameNumber = 1, SpreadPick = "Alabama", ConfidencePoints = 1, OutrightWinner = "Georgia" },
                new BowlPick { GameNumber = 2, SpreadPick = "Georgia", ConfidencePoints = 2, OutrightWinner = "Alabama" },
                new BowlPick { GameNumber = 3, SpreadPick = "Ohio State", ConfidencePoints = 3, OutrightWinner = "Michigan" }
            }
        };

        // Act
        var result = await _service.GenerateBowlPicksExcelAsync(userPicks);

        // Verify by reading the Excel
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var stream = new MemoryStream(result);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];

        // Assert
        worksheet.Cells[1, 2].Value.Should().Be("John Doe");
        worksheet.Cells[4, 2].Value.Should().Be("Alabama");
        worksheet.Cells[4, 3].Value.Should().Be(1);
        worksheet.Cells[4, 4].Value.Should().Be("Georgia");
        worksheet.Cells[5, 2].Value.Should().Be("Georgia");
        worksheet.Cells[5, 4].Value.Should().Be("Alabama");
        worksheet.Cells[6, 2].Value.Should().Be("Ohio State");
        worksheet.Cells[6, 4].Value.Should().Be("Michigan");
    }

    [Fact]
    public async Task GenerateBowlPicksExcelAsync_InvalidPicks_ThrowsArgumentException()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "", // Invalid - empty name
            Year = 2024,
            TotalGames = 36,
            Picks = CreateValidPicks(36)
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GenerateBowlPicksExcelAsync(userPicks));
    }

    [Fact]
    public async Task GenerateBowlPicksExcelAsync_HasConfidenceSumFormula()
    {
        // Arrange
        var userPicks = new BowlUserPicks
        {
            Name = "John Doe",
            Year = 2024,
            TotalGames = 3,
            Picks = new List<BowlPick>
            {
                new BowlPick { GameNumber = 1, SpreadPick = "Alabama", ConfidencePoints = 1, OutrightWinner = "Alabama" },
                new BowlPick { GameNumber = 2, SpreadPick = "Georgia", ConfidencePoints = 2, OutrightWinner = "Georgia" },
                new BowlPick { GameNumber = 3, SpreadPick = "Ohio State", ConfidencePoints = 3, OutrightWinner = "Ohio State" }
            }
        };

        // Act
        var result = await _service.GenerateBowlPicksExcelAsync(userPicks);

        // Verify by reading the Excel
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var stream = new MemoryStream(result);
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];

        // Assert - Check for sum formula (row 8: empty row after data, so sum is in row 8)
        var sumFormula = worksheet.Cells[8, 3].Formula;
        sumFormula.Should().Contain("SUM");
    }

    [Fact]
    public async Task ParseBowlLinesAsync_RealBowlLines2File_ParsesCorrectly()
    {
        // Arrange - Load the actual Bowl Lines (2).xlsx file from reference-docs
        var projectRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".."));
        var filePath = Path.Combine(projectRoot, "reference-docs", "Bowl-Lines-2.xlsx");
        
        // Skip test if file doesn't exist (for CI environments without the file)
        if (!File.Exists(filePath))
        {
            return;
        }

        using var fileStream = File.OpenRead(filePath);

        // Act
        var result = await _service.ParseBowlLinesAsync(fileStream, 2025);

        // Assert
        result.Should().NotBeNull();
        result.Year.Should().Be(2025);
        result.Games.Should().HaveCount(35); // The actual file has 35 games
        result.TotalGames.Should().Be(35);

        // Expected confidence sum = N * (N + 1) / 2 = 35 * 36 / 2 = 630
        var expectedSum = result.TotalGames * (result.TotalGames + 1) / 2;
        expectedSum.Should().Be(630);

        // Verify first game
        var firstGame = result.Games[0];
        firstGame.GameNumber.Should().Be(1);
        firstGame.Favorite.Should().Be("Washington");
        firstGame.Line.Should().Be(-8.5m);
        firstGame.Underdog.Should().Be("Boise State");

        // Verify last game
        var lastGame = result.Games[34];
        lastGame.GameNumber.Should().Be(35);
        lastGame.Favorite.Should().Be("Mississippi State");
        lastGame.Line.Should().Be(-3m);
        lastGame.Underdog.Should().Be("Wake Forest");
    }
}
