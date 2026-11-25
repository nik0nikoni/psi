using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace TicketGenerator.E2ETests;

public sealed class ApplicationE2ETests : IDisposable
{
    private static readonly string[] Header = ["Last name", "First name", "Номер билета", "Дата и время"];

    private readonly string _directory;
    private readonly string _journalPath;

    public ApplicationE2ETests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "ticket-e2e-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _journalPath = Path.Combine(_directory, "journal.xlsx");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void ThreeStudents_CreatesJournalWithHeaderAndThreeRows()
    {
        var result = ApplicationProcess.Run(
            _directory,
            "Иванов", "Иван",
            "Петров", "Пётр",
            "Сидоров", "Сидор");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.StartsWith("Для выхода нажмите ESC.", result.Output);
        Assert.Contains("Last name: ", result.Output);
        Assert.Contains("First name: ", result.Output);
        var ticketNumbers = Regex.Matches(result.Output, @"Билет № (\d+)")
            .Select(match => int.Parse(match.Groups[1].Value))
            .ToList();
        Assert.Equal(3, ticketNumbers.Count);
        Assert.All(ticketNumbers, number => Assert.InRange(number, 1, 20));

        var rows = ReadJournal();
        Assert.Equal(4, rows.Count);
        Assert.Equal(Header, rows[0]);
        Assert.Equal(["Иванов", "Иван", ticketNumbers[0].ToString()], rows[1][..3]);
        Assert.Equal(["Петров", "Пётр", ticketNumbers[1].ToString()], rows[2][..3]);
        Assert.Equal(["Сидоров", "Сидор", ticketNumbers[2].ToString()], rows[3][..3]);
    }

    [Fact]
    public void SecondRun_AppendsTwoRowsAndKeepsPreviousOnes()
    {
        ApplicationProcess.Run(_directory, "Иванов", "Иван", "Петров", "Пётр", "Сидоров", "Сидор");
        var firstRunRows = ReadJournal();

        var result = ApplicationProcess.Run(_directory, "Кузнецов", "Кузьма", "Смирнов", "Семён");

        Assert.Equal(0, result.ExitCode);
        var rows = ReadJournal();
        Assert.Equal(6, rows.Count);
        Assert.Equal(firstRunRows, rows.Take(4));
        Assert.Equal(["Кузнецов", "Кузьма"], rows[4][..2]);
        Assert.Equal(["Смирнов", "Семён"], rows[5][..2]);
    }

    [Fact]
    public void EmptyInput_RepeatsPromptAndTrimsNames()
    {
        var result = ApplicationProcess.Run(_directory, "", "   ", "  Иванов  ", "", " Иван ");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(3, Regex.Matches(result.Output, "Значение не может быть пустым").Count);
        var rows = ReadJournal();
        Assert.Equal(2, rows.Count);
        Assert.Equal(["Иванов", "Иван"], rows[1][..2]);
    }

    [Fact]
    public void KilledAfterStudent_RecordStaysInJournal()
    {
        using var process = ApplicationProcess.Start(_directory);
        process.StandardInput.Write("Иванов\nИван\n");
        process.StandardInput.Flush();

        WaitForOutput(process, "Билет №");
        process.Kill(entireProcessTree: true);
        process.WaitForExit();

        var rows = ReadJournal();
        Assert.Equal(2, rows.Count);
        Assert.Equal(["Иванов", "Иван"], rows[1][..2]);
    }

    [Fact]
    public void LockedJournal_PrintsMessageAndExitsNormally()
    {
        ApplicationProcess.Run(_directory, "Иванов", "Иван");

        ApplicationResult result;
        using (new FileStream(_journalPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            result = ApplicationProcess.Run(_directory, "Петров", "Пётр");
        }

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.Contains("недоступен для записи", result.Output);
        Assert.Contains("Закройте файл", result.Output);
        Assert.DoesNotContain("Билет №", result.Output);
        Assert.Equal(2, ReadJournal().Count);
    }

    private static void WaitForOutput(System.Diagnostics.Process process, string expected)
    {
        var output = new System.Text.StringBuilder();
        var buffer = new char[256];
        var deadline = DateTime.UtcNow.AddSeconds(60);

        while (!output.ToString().Contains(expected))
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Expected output \"{expected}\" was not received.");
            }

            var read = process.StandardOutput.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                throw new InvalidOperationException("The application exited before printing the expected output.");
            }

            output.Append(buffer, 0, read);
        }
    }

    private List<string[]> ReadJournal()
    {
        using var workbook = new XLWorkbook(_journalPath);
        return workbook.Worksheets.First().RowsUsed()
            .Select(row => Enumerable.Range(1, 4).Select(column => row.Cell(column).GetString()).ToArray())
            .ToList();
    }
}
