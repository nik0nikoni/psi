using ClosedXML.Excel;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;
using ExamTickets.Infrastructure.Excel;

namespace ExamTickets.UnitTests.Excel;

public sealed class ExcelResultJournalTests : IDisposable
{
    private static readonly Student Ivanov = new("Иванов", "Иван");
    private static readonly Student Petrov = new("Петров", "Пётр");

    private readonly string _directory;
    private readonly string _filePath;

    public ExcelResultJournalTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "exam-tickets-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "results.xlsx");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Append_MissingFile_CreatesFileWithHeaderAndRecord()
    {
        var generatedAt = new DateTime(2026, 11, 25, 10, 15, 30);

        new ExcelResultJournal(_filePath).Append(Record("TI-221", Ivanov, 7, generatedAt, isRepeat: false));

        using var workbook = new XLWorkbook(_filePath);
        var worksheet = workbook.Worksheets.First();
        Assert.Equal(
            ["Группа", "Фамилия", "Имя", "Номер билета", "Дата и время", "Повтор (да/нет)"],
            Enumerable.Range(1, 6).Select(column => worksheet.Cell(1, column).GetString()));
        Assert.Equal("TI-221", worksheet.Cell(2, 1).GetString());
        Assert.Equal("Иванов", worksheet.Cell(2, 2).GetString());
        Assert.Equal("Иван", worksheet.Cell(2, 3).GetString());
        Assert.Equal(7, worksheet.Cell(2, 4).GetValue<int>());
        Assert.Equal(generatedAt, worksheet.Cell(2, 5).GetDateTime());
        Assert.Equal("нет", worksheet.Cell(2, 6).GetString());
        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void EnsureCreated_MissingFile_CreatesFileWithHeaderOnly()
    {
        new ExcelResultJournal(_filePath).EnsureCreated();

        Assert.Equal(
            [["Группа", "Фамилия", "Имя", "Номер билета", "Дата и время", "Повтор (да/нет)"]],
            ReadRows());
    }

    [Fact]
    public void EnsureCreated_ExistingFile_KeepsRows()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-221", Ivanov, 7, new DateTime(2026, 11, 25, 10, 0, 0), isRepeat: false));
        var rowsBefore = ReadRows();

        journal.EnsureCreated();

        Assert.Equal(rowsBefore, ReadRows());
    }

    [Fact]
    public void Append_RepeatRecord_WritesYes()
    {
        new ExcelResultJournal(_filePath).Append(Record("TI-221", Ivanov, 7, DateTime.Now, isRepeat: true));

        using var workbook = new XLWorkbook(_filePath);
        Assert.Equal("да", workbook.Worksheets.First().Cell(2, 6).GetString());
    }

    [Fact]
    public void Append_ExistingFile_AddsRowAtEndWithoutChangingExistingRows()
    {
        var journal = new ExcelResultJournal(_filePath);
        var first = new DateTime(2026, 11, 25, 10, 0, 0);
        journal.Append(Record("TI-221", Ivanov, 3, first, isRepeat: false));
        journal.Append(Record("TI-221", Petrov, 5, first.AddMinutes(1), isRepeat: false));
        var rowsBefore = ReadRows();

        journal.Append(Record("TI-221", Ivanov, 3, first.AddMinutes(2), isRepeat: true));

        var rowsAfter = ReadRows();
        Assert.Equal(4, rowsAfter.Count);
        Assert.Equal(rowsBefore, rowsAfter.Take(rowsBefore.Count));
        Assert.Equal(["TI-221", "Иванов", "Иван", "3", "25.11.2026 10:02:00", "да"], rowsAfter[3]);
    }

    [Fact]
    public void FindFirst_MissingFile_ReturnsNull()
    {
        var record = new ExcelResultJournal(_filePath).FindFirst("TI-221", Ivanov);

        Assert.Null(record);
    }

    [Fact]
    public void FindFirst_SeveralRecordsOfStudent_ReturnsEarliestOne()
    {
        var journal = new ExcelResultJournal(_filePath);
        var first = new DateTime(2026, 11, 25, 10, 0, 0);
        journal.Append(Record("TI-221", Petrov, 1, first, isRepeat: false));
        journal.Append(Record("TI-221", Ivanov, 4, first.AddMinutes(1), isRepeat: false));
        journal.Append(Record("TI-221", Ivanov, 4, first.AddMinutes(2), isRepeat: true));

        var record = journal.FindFirst("TI-221", Ivanov);

        Assert.Equal(Record("TI-221", Ivanov, 4, first.AddMinutes(1), isRepeat: false), record);
    }

    [Fact]
    public void FindFirst_SameNameInAnotherGroup_IsNotMatched()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-222", Ivanov, 4, DateTime.Now, isRepeat: false));

        Assert.Null(journal.FindFirst("TI-221", Ivanov));
    }

    [Fact]
    public void FindFirst_OnlyOtherStudents_ReturnsNull()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-221", Petrov, 4, DateTime.Now, isRepeat: false));

        Assert.Null(journal.FindFirst("TI-221", Ivanov));
    }

    [Fact]
    public void FindFirst_FileOpenedByAnotherProcess_StillReads()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-221", Ivanov, 4, DateTime.Now, isRepeat: false));
        using var openedElsewhere = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        Assert.Equal(4, journal.FindFirst("TI-221", Ivanov)?.TicketNumber);
    }

    [Fact]
    public void Append_LockedFile_ThrowsJournalUnavailableException()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-221", Ivanov, 1, DateTime.Now, isRepeat: false));

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var exception = Assert.Throws<JournalUnavailableException>(
                () => journal.Append(Record("TI-221", Petrov, 2, DateTime.Now, isRepeat: false)));

            Assert.Contains("Закройте файл", exception.Message);
        }

        Assert.False(File.Exists(Path.ChangeExtension(_filePath, ".tmp.xlsx")));
    }

    [Fact]
    public void FindFirst_LockedFile_ThrowsJournalUnavailableException()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-221", Ivanov, 1, DateTime.Now, isRepeat: false));

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<JournalUnavailableException>(() => journal.FindFirst("TI-221", Ivanov));
        }
    }

    [Fact]
    public void Append_AfterLockIsReleased_Succeeds()
    {
        var journal = new ExcelResultJournal(_filePath);
        journal.Append(Record("TI-221", Ivanov, 1, DateTime.Now, isRepeat: false));

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Throws<JournalUnavailableException>(
                () => journal.Append(Record("TI-221", Petrov, 2, DateTime.Now, isRepeat: false)));
        }

        journal.Append(Record("TI-221", Petrov, 2, DateTime.Now, isRepeat: false));

        using var workbook = new XLWorkbook(_filePath);
        Assert.Equal(3, workbook.Worksheets.First().LastRowUsed()!.RowNumber());
    }

    private static ResultRecord Record(string group, Student student, int ticketNumber, DateTime generatedAt, bool isRepeat) =>
        new(group, student.LastName, student.FirstName, ticketNumber, generatedAt, isRepeat);

    private List<string[]> ReadRows()
    {
        using var workbook = new XLWorkbook(_filePath);
        var worksheet = workbook.Worksheets.First();

        return worksheet.RowsUsed()
            .Select(row => Enumerable.Range(1, 6).Select(column => row.Cell(column).GetFormattedString()).ToArray())
            .ToList();
    }
}
