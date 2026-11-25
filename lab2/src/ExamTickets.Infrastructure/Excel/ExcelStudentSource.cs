using ClosedXML.Excel;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;

namespace ExamTickets.Infrastructure.Excel;

public sealed class ExcelStudentSource : IStudentSource
{
    private const int FirstDataRow = 2;
    private const int LastNameColumn = 1;
    private const int FirstNameColumn = 2;

    private readonly string _filePath;

    public ExcelStudentSource(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<string> GetGroups()
    {
        using var workbook = OpenWorkbook();
        return workbook.Worksheets.Select(worksheet => worksheet.Name).ToList();
    }

    public IReadOnlyList<Student> GetStudents(string group)
    {
        using var workbook = OpenWorkbook();
        if (!workbook.TryGetWorksheet(group, out var worksheet))
        {
            return [];
        }

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var students = new List<Student>();

        for (var row = FirstDataRow; row <= lastRow; row++)
        {
            var lastName = worksheet.Cell(row, LastNameColumn).GetFormattedString().Trim();
            var firstName = worksheet.Cell(row, FirstNameColumn).GetFormattedString().Trim();

            if (lastName.Length == 0 && firstName.Length == 0)
            {
                continue;
            }

            students.Add(new Student(lastName, firstName));
        }

        return students;
    }

    private XLWorkbook OpenWorkbook()
    {
        if (!File.Exists(_filePath))
        {
            throw new SourceFileMissingException(_filePath);
        }

        // FileShare.ReadWrite lets the list be read while it is open in Excel.
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new XLWorkbook(stream);
    }
}
