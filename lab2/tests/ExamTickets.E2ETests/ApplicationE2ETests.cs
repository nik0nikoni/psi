using System.Text.RegularExpressions;
using ClosedXML.Excel;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ExamTickets.E2ETests;

public sealed class ApplicationE2ETests : IDisposable
{
    private static readonly string[] Header =
        ["Группа", "Фамилия", "Имя", "Номер билета", "Дата и время", "Повтор (да/нет)"];

    private static readonly int[] ValidTicketNumbers = [1, 2, 3, 4, 5, 8, 9, 10];

    private readonly string _directory;
    private readonly string _resultsPath;

    public ApplicationE2ETests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "exam-tickets-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _resultsPath = Path.Combine(_directory, "results.xlsx");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Start_ShowsGroupsAndCreatesJournalWithHeader()
    {
        CopySamples();
        using var session = ApplicationSession.Start(_directory);

        var main = session.GetMainWindow();

        Assert.Equal(["TI-221", "TI-222", "FAF-231", "Пустая группа"], main.GroupNames);
        Assert.False(main.GenerateButton.IsEnabled);
        Assert.Equal([Header], ReadResults());
    }

    [Fact]
    public void Criteria1And2_GroupFillsStudentsAndButtonNeedsBothSelections()
    {
        CopySamples();
        using var session = ApplicationSession.Start(_directory);
        var main = session.GetMainWindow();

        main.SelectGroup("TI-222");

        Assert.Equal(["Смирнов Сергей", "Волкова Елена", "Морозов Андрей", "Новикова Ольга"], main.StudentNames);
        Assert.False(main.GenerateButton.IsEnabled);

        main.SelectStudent("Волкова Елена");

        Assert.True(main.GenerateButton.IsEnabled);
    }

    [Fact]
    public void EmptyGroup_ShowsNoStudentsAndKeepsButtonDisabled()
    {
        CopySamples();
        using var session = ApplicationSession.Start(_directory);
        var main = session.GetMainWindow();

        main.SelectGroup("Пустая группа");

        Assert.Empty(main.StudentNames);
        Assert.False(main.GenerateButton.IsEnabled);
        Assert.False(session.HasExited);
    }

    [Fact]
    public void Criteria3To6_GenerateShowsTicketEscClosesOnlyResultAndRowIsSavedAtOnce()
    {
        CopySamples();
        using var session = ApplicationSession.Start(_directory);
        var main = session.GetMainWindow();
        main.SelectGroup("TI-221");
        main.SelectStudent("Иванов Иван");

        var result = main.Generate();

        var number = ParseTicketNumber(result.Title);
        Assert.Contains(number, ValidTicketNumbers);
        Assert.Equal(3, result.Questions.Count);
        Assert.All(result.Questions, question => Assert.Matches(@"^\d\. .+", question));

        var rows = ReadResults();
        Assert.Equal(2, rows.Count);
        Assert.Equal(["TI-221", "Иванов", "Иван", number.ToString(), rows[1][4], "нет"], rows[1]);

        result.PressEscape();

        Assert.True(main.WaitUntilNoModalWindow());
        Assert.False(session.HasExited);
        Assert.True(main.GenerateButton.IsEnabled);
    }

    [Fact]
    public void Criteria4And8And9_RepeatedStudentGetsSameTicketAndJournalKeepsHistory()
    {
        CopySamples();
        var firstSessionRows = new List<string[]>();
        int ivanovTicket;

        using (var session = ApplicationSession.Start(_directory))
        {
            var main = session.GetMainWindow();
            ivanovTicket = GenerateAndClose(main, "TI-221", "Иванов Иван");
            GenerateAndClose(main, "FAF-231", "Козлова Дарья");
            Assert.Equal(ivanovTicket, GenerateAndClose(main, "TI-221", "Иванов Иван"));
            firstSessionRows.AddRange(ReadResults());
        }

        // The ticket is looked up in results.xlsx, so it survives a restart of the application.
        using (var session = ApplicationSession.Start(_directory))
        {
            Assert.Equal(ivanovTicket, GenerateAndClose(session.GetMainWindow(), "TI-221", "Иванов Иван"));
        }

        var rows = ReadResults();
        Assert.Equal(5, rows.Count);
        Assert.Equal(firstSessionRows, rows.Take(firstSessionRows.Count));
        Assert.Equal(
            ["TI-221/Иванов/нет", "FAF-231/Козлова/нет", "TI-221/Иванов/да", "TI-221/Иванов/да"],
            rows.Skip(1).Select(row => $"{row[0]}/{row[1]}/{row[5]}"));
        Assert.All(
            rows.Skip(1).Where(row => row[1] == "Иванов"),
            row => Assert.Equal(ivanovTicket.ToString(), row[3]));

        var times = ReadGenerationTimes();
        Assert.Equal(times.Order(), times);
    }

    [Fact]
    public void Criterion7_LockedJournal_AsksToRetryAndSavesAfterUnlock()
    {
        CopySamples();
        using var session = ApplicationSession.Start(_directory);
        var main = session.GetMainWindow();
        main.SelectGroup("TI-221");
        main.SelectStudent("Петрова Мария");

        using (new FileStream(_resultsPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            main.GenerateButton.Invoke();
            var question = main.WaitForModalWindow();
            Assert.Contains("Повторить попытку?", TextOf(question));
            Assert.False(session.HasExited);
        }

        ClickMessageBoxButton(main.WaitForModalWindow(), "6"); // IDYES

        var result = new ResultWindowDriver(main.WaitForModalWindow());
        var number = ParseTicketNumber(result.Title);
        result.PressEscape();

        var rows = ReadResults();
        Assert.Equal(2, rows.Count);
        Assert.Equal(["TI-221", "Петрова", "Мария", number.ToString()], rows[1][..4]);
    }

    [Fact]
    public void Criterion7_MissingSourceFiles_ShowMessageAndExitWithoutCrash()
    {
        using var session = ApplicationSession.Start(_directory);

        var message = session.WaitForTopLevelWindow();

        Assert.Contains("tickets.docx", TextOf(message));
        ClickMessageBoxButton(message, "2"); // IDOK closes the message box
        Assert.True(session.WaitForExit());
        Assert.Equal(1, session.ExitCode);
    }

    private static int GenerateAndClose(MainWindowDriver main, string group, string student)
    {
        main.SelectGroup(group);
        main.SelectStudent(student);

        var result = main.Generate();
        var number = ParseTicketNumber(result.Title);
        result.PressEscape();
        Assert.True(main.WaitUntilNoModalWindow());

        return number;
    }

    private static int ParseTicketNumber(string title)
    {
        var match = Regex.Match(title, @"^Ваш билет № (\d+)$");
        Assert.True(match.Success, $"Unexpected title: {title}");
        return int.Parse(match.Groups[1].Value);
    }

    private static string TextOf(Window window) =>
        string.Join(
            Environment.NewLine,
            window.FindAllDescendants(condition => condition.ByControlType(ControlType.Text))
                .Select(element => element.Name));

    private static void ClickMessageBoxButton(Window messageBox, string automationId)
    {
        var button = messageBox.FindFirstDescendant(condition => condition.ByAutomationId(automationId))?.AsButton()
            ?? messageBox.FindFirstDescendant(condition => condition.ByControlType(ControlType.Button)).AsButton();
        button.Invoke();
    }

    private void CopySamples()
    {
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "samples")))
        {
            File.Copy(file, Path.Combine(_directory, Path.GetFileName(file)));
        }
    }

    private List<string[]> ReadResults()
    {
        using var workbook = OpenResults();
        return workbook.Worksheets.First().RowsUsed()
            .Select(row => Enumerable.Range(1, 6).Select(column => row.Cell(column).GetFormattedString()).ToArray())
            .ToList();
    }

    private List<DateTime> ReadGenerationTimes()
    {
        using var workbook = OpenResults();
        return workbook.Worksheets.First().RowsUsed().Skip(1)
            .Select(row => row.Cell(5).GetDateTime())
            .ToList();
    }

    private XLWorkbook OpenResults()
    {
        using var stream = new FileStream(_resultsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new XLWorkbook(stream);
    }
}
