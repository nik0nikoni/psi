using ClosedXML.Excel;
using TicketGenerator.Core.Exceptions;
using TicketGenerator.Core.Models;
using TicketGenerator.Infrastructure.Excel;

namespace TicketGenerator.UnitTests;

public sealed class ExcelTicketRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public ExcelTicketRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ticket-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "journal.xlsx");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Append_MissingFile_CreatesFileWithHeaderAndRecord()
    {
        var repository = new ExcelTicketRepository(_filePath);
        var issuedAt = new DateTime(2026, 10, 28, 12, 30, 15);

        repository.Append(new TicketRecord("Иванов", "Иван", 7, issuedAt));

        using var workbook = new XLWorkbook(_filePath);
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Last name", worksheet.Cell(1, 1).GetString());
        Assert.Equal("First name", worksheet.Cell(1, 2).GetString());
        Assert.Equal("Номер билета", worksheet.Cell(1, 3).GetString());
        Assert.Equal("Дата и время", worksheet.Cell(1, 4).GetString());
        Assert.Equal("Иванов", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Иван", worksheet.Cell(2, 2).GetString());
        Assert.Equal(7, worksheet.Cell(2, 3).GetValue<int>());
        Assert.Equal(issuedAt, worksheet.Cell(2, 4).GetDateTime());
        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void Append_ExistingFile_AddsRowsAfterExistingOnes()
    {
        var issuedAt = new DateTime(2026, 10, 28, 12, 30, 0);
        new ExcelTicketRepository(_filePath).Append(new TicketRecord("Иванов", "Иван", 1, issuedAt));
        new ExcelTicketRepository(_filePath).Append(new TicketRecord("Петров", "Пётр", 2, issuedAt));

        new ExcelTicketRepository(_filePath).Append(new TicketRecord("Сидоров", "Сидор", 3, issuedAt));

        using var workbook = new XLWorkbook(_filePath);
        var worksheet = workbook.Worksheets.First();
        Assert.Equal(4, worksheet.LastRowUsed()!.RowNumber());
        Assert.Equal("Last name", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Иванов", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Петров", worksheet.Cell(3, 1).GetString());
        Assert.Equal("Сидоров", worksheet.Cell(4, 1).GetString());
    }

    [Fact]
    public void Append_LockedFile_ThrowsJournalUnavailableException()
    {
        var repository = new ExcelTicketRepository(_filePath);
        repository.Append(new TicketRecord("Иванов", "Иван", 1, DateTime.Now));

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var exception = Assert.Throws<JournalUnavailableException>(
                () => repository.Append(new TicketRecord("Петров", "Пётр", 2, DateTime.Now)));

            Assert.Contains("Закройте файл", exception.Message);
        }

        Assert.False(File.Exists(Path.ChangeExtension(_filePath, ".tmp.xlsx")));
    }

    [Fact]
    public void Append_AfterLockIsReleased_Succeeds()
    {
        var repository = new ExcelTicketRepository(_filePath);
        repository.Append(new TicketRecord("Иванов", "Иван", 1, DateTime.Now));

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<JournalUnavailableException>(
                () => repository.Append(new TicketRecord("Петров", "Пётр", 2, DateTime.Now)));
        }

        repository.Append(new TicketRecord("Петров", "Пётр", 2, DateTime.Now));

        using var workbook = new XLWorkbook(_filePath);
        Assert.Equal(3, workbook.Worksheets.First().LastRowUsed()!.RowNumber());
    }
}
