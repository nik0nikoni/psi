using ClosedXML.Excel;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;
using ExamTickets.Infrastructure.Excel;

namespace ExamTickets.UnitTests.Excel;

public sealed class ExcelStudentSourceTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public ExcelStudentSourceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "exam-tickets-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "students.xlsx");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void GetGroups_ReturnsWorksheetNamesInOrder()
    {
        CreateWorkbook(
            ("TI-221", [("Иванов", "Иван")]),
            ("TI-222", []),
            ("FAF-231", [("Петров", "Пётр")]));

        var groups = new ExcelStudentSource(_filePath).GetGroups();

        Assert.Equal(["TI-221", "TI-222", "FAF-231"], groups);
    }

    [Fact]
    public void GetStudents_ReturnsStudentsOfSelectedSheetOnly()
    {
        CreateWorkbook(
            ("TI-221", [("Иванов", "Иван"), ("Сидорова", "Анна")]),
            ("TI-222", [("Петров", "Пётр")]));

        var students = new ExcelStudentSource(_filePath).GetStudents("TI-221");

        Assert.Equal([new Student("Иванов", "Иван"), new Student("Сидорова", "Анна")], students);
    }

    [Fact]
    public void GetStudents_SkipsHeaderBlankRowsAndTrimsNames()
    {
        CreateWorkbook(("TI-221", [("  Иванов ", " Иван"), ("", ""), ("Петров", "Пётр")]));

        var students = new ExcelStudentSource(_filePath).GetStudents("TI-221");

        Assert.Equal([new Student("Иванов", "Иван"), new Student("Петров", "Пётр")], students);
    }

    [Fact]
    public void GetStudents_EmptyGroup_ReturnsEmptyList()
    {
        CreateWorkbook(("TI-221", [("Иванов", "Иван")]), ("Пустая", []));

        var students = new ExcelStudentSource(_filePath).GetStudents("Пустая");

        Assert.Empty(students);
    }

    [Fact]
    public void GetStudents_UnknownGroup_ReturnsEmptyList()
    {
        CreateWorkbook(("TI-221", [("Иванов", "Иван")]));

        var students = new ExcelStudentSource(_filePath).GetStudents("Нет такой");

        Assert.Empty(students);
    }

    [Fact]
    public void GetStudents_FileOpenedByAnotherProcess_StillReads()
    {
        CreateWorkbook(("TI-221", [("Иванов", "Иван")]));
        using var openedElsewhere = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        var students = new ExcelStudentSource(_filePath).GetStudents("TI-221");

        Assert.Equal([new Student("Иванов", "Иван")], students);
    }

    [Fact]
    public void GetGroups_MissingFile_ThrowsSourceFileMissingException()
    {
        var source = new ExcelStudentSource(_filePath);

        var exception = Assert.Throws<SourceFileMissingException>(() => source.GetGroups());

        Assert.Equal(_filePath, exception.FilePath);
    }

    [Fact]
    public void GetStudents_MissingFile_ThrowsSourceFileMissingException()
    {
        var source = new ExcelStudentSource(_filePath);

        Assert.Throws<SourceFileMissingException>(() => source.GetStudents("TI-221"));
    }

    private void CreateWorkbook(params (string Group, (string LastName, string FirstName)[] Students)[] groups)
    {
        using var workbook = new XLWorkbook();

        foreach (var (group, students) in groups)
        {
            var worksheet = workbook.AddWorksheet(group);
            worksheet.Cell(1, 1).Value = "Фамилия";
            worksheet.Cell(1, 2).Value = "Имя";

            for (var i = 0; i < students.Length; i++)
            {
                worksheet.Cell(i + 2, 1).Value = students[i].LastName;
                worksheet.Cell(i + 2, 2).Value = students[i].FirstName;
            }
        }

        workbook.SaveAs(_filePath);
    }
}
