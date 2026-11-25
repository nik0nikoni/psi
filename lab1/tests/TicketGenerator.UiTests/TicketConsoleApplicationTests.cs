using ClosedXML.Excel;
using NSubstitute;
using TicketGenerator.ConsoleApp;
using TicketGenerator.ConsoleApp.Input;
using TicketGenerator.Core.Abstractions;
using TicketGenerator.Core.Generation;
using TicketGenerator.Core.Services;
using TicketGenerator.Core.Validation;
using TicketGenerator.Infrastructure.Excel;

namespace TicketGenerator.UiTests;

public sealed class TicketConsoleApplicationTests : IDisposable
{
    private readonly string _directory;
    private readonly string _journalPath;
    private readonly IRandomNumberSource _randomSource = Substitute.For<IRandomNumberSource>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public TicketConsoleApplicationTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ticket-ui-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _journalPath = Path.Combine(_directory, "journal.xlsx");
        _randomSource.Next(1, 21).Returns(7);
        _clock.Now.Returns(new DateTime(2026, 10, 28, 12, 30, 0));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void ThreeStudentsThenEscape_JournalContainsHeaderAndThreeRows()
    {
        var console = new FakeConsole()
            .TypeLine("Иванов").TypeLine("Иван")
            .TypeLine("Петров").TypeLine("Пётр")
            .TypeLine("Сидоров").TypeLine("Сидор")
            .PressEscape();

        Run(console);

        Assert.StartsWith(TicketConsoleApplication.ExitHint, console.Output);
        Assert.Equal(3, CountOccurrences(console.Output, "Билет № 7"));
        var rows = ReadJournal();
        Assert.Equal(ExcelTicketRepository.Headers, rows[0]);
        Assert.Equal(4, rows.Count);
        Assert.Equal(["Иванов", "Иван", "7"], rows[1][..3]);
        Assert.Equal(["Петров", "Пётр", "7"], rows[2][..3]);
        Assert.Equal(["Сидоров", "Сидор", "7"], rows[3][..3]);
    }

    [Fact]
    public void SecondRunWithTwoStudents_AppendsRowsAndKeepsFirstThree()
    {
        Run(new FakeConsole()
            .TypeLine("Иванов").TypeLine("Иван")
            .TypeLine("Петров").TypeLine("Пётр")
            .TypeLine("Сидоров").TypeLine("Сидор")
            .PressEscape());
        var firstRunRows = ReadJournal();

        Run(new FakeConsole()
            .TypeLine("Кузнецов").TypeLine("Кузьма")
            .TypeLine("Смирнов").TypeLine("Семён")
            .PressEscape());

        var rows = ReadJournal();
        Assert.Equal(6, rows.Count);
        Assert.Equal(firstRunRows, rows.Take(4));
        Assert.Equal("Кузнецов", rows[4][0]);
        Assert.Equal("Смирнов", rows[5][0]);
    }

    [Fact]
    public void EmptyLastName_RepeatsPrompt()
    {
        var console = new FakeConsole()
            .TypeLine(string.Empty)
            .TypeLine("   ")
            .TypeLine("Иванов").TypeLine("Иван")
            .PressEscape();

        Run(console);

        Assert.Equal(2, CountOccurrences(console.Output, TicketConsoleApplication.EmptyValueMessage));
        Assert.Equal(4, CountOccurrences(console.Output, TicketConsoleApplication.LastNamePrompt));
        Assert.Equal(2, ReadJournal().Count);
    }

    [Fact]
    public void EmptyFirstName_RepeatsFirstNamePromptOnly()
    {
        var console = new FakeConsole()
            .TypeLine("Иванов")
            .TypeLine(string.Empty)
            .TypeLine("Иван")
            .PressEscape();

        Run(console);

        Assert.Equal(1, CountOccurrences(console.Output, TicketConsoleApplication.EmptyValueMessage));
        Assert.Equal(2, CountOccurrences(console.Output, TicketConsoleApplication.FirstNamePrompt));
        Assert.Equal(["Иванов", "Иван", "7"], ReadJournal()[1][..3]);
    }

    [Fact]
    public void NamesAreTrimmedAndBackspaceRemovesCharacters()
    {
        var console = new FakeConsole()
            .TypeLine("  Иванов  ")
            .Type("Ивн").PressBackspace().TypeLine("ан")
            .PressEscape();

        Run(console);

        Assert.Equal(["Иванов", "Иван", "7"], ReadJournal()[1][..3]);
    }

    [Fact]
    public void LockedJournal_ShowsMessageAndDoesNotCrash()
    {
        FileStream? lockStream = null;
        var console = new FakeConsole()
            .TypeLine("Иванов").TypeLine("Иван")
            .Then(() => lockStream = LockJournal())
            .TypeLine("Петров").TypeLine("Пётр")
            .PressEscape();

        try
        {
            var exception = Record.Exception(() => Run(console));

            Assert.Null(exception);
        }
        finally
        {
            lockStream?.Dispose();
        }

        Assert.Contains("недоступен для записи", console.Output);
        Assert.Contains(TicketConsoleApplication.RetryPrompt, console.Output);
        Assert.Equal(2, ReadJournal().Count);
    }

    [Fact]
    public void LockedJournal_RetryAfterClosingFile_SavesStudent()
    {
        FileStream? lockStream = null;
        var console = new FakeConsole()
            .TypeLine("Иванов").TypeLine("Иван")
            .Then(() => lockStream = LockJournal())
            .TypeLine("Петров").TypeLine("Пётр")
            .Then(() => lockStream!.Dispose())
            .TypeLine(string.Empty)
            .PressEscape();

        Run(console);

        Assert.Contains("недоступен для записи", console.Output);
        Assert.Equal(2, CountOccurrences(console.Output, "Билет № 7"));
        var rows = ReadJournal();
        Assert.Equal(3, rows.Count);
        Assert.Equal("Петров", rows[2][0]);
    }

    [Fact]
    public void EscapeAtStart_ExitsWithoutCreatingJournal()
    {
        var console = new FakeConsole().PressEscape();

        Run(console);

        Assert.False(File.Exists(_journalPath));
    }

    private void Run(FakeConsole console)
    {
        var validator = new StudentNameValidator();
        var service = new TicketIssuingService(
            validator,
            new TicketNumberGenerator(_randomSource),
            _clock,
            new ExcelTicketRepository(_journalPath));

        new TicketConsoleApplication(console, new ConsoleLineReader(console), validator, service).Run();
    }

    private FileStream LockJournal() =>
        new(_journalPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

    private List<string[]> ReadJournal()
    {
        using var workbook = new XLWorkbook(_journalPath);
        return workbook.Worksheets.First().RowsUsed()
            .Select(row => Enumerable.Range(1, 4).Select(column => row.Cell(column).GetString()).ToArray())
            .ToList();
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var index = text.IndexOf(value, StringComparison.Ordinal);
             index >= 0;
             index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
