using ClosedXML.Excel;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;

namespace ExamTickets.Infrastructure.Excel;

public sealed class ExcelResultJournal : IResultJournal
{
    public const string WorksheetName = "Результаты";
    public const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";
    public const string RepeatYes = "да";
    public const string RepeatNo = "нет";

    public static readonly IReadOnlyList<string> Headers =
        ["Группа", "Фамилия", "Имя", "Номер билета", "Дата и время", "Повтор (да/нет)"];

    private const int FirstDataRow = 2;

    private readonly string _filePath;

    public ExcelResultJournal(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
    }

    // Creates the journal with its header on application start; an existing file is left untouched.
    public void EnsureCreated()
    {
        if (File.Exists(_filePath))
        {
            return;
        }

        try
        {
            using var workbook = CreateWorkbook();
            Save(workbook);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new JournalUnavailableException(_filePath, exception);
        }
    }

    public ResultRecord? FindFirst(string group, Student student)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            // FileShare.ReadWrite lets the journal be searched while it is open in Excel.
            using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (var row = FirstDataRow; row <= lastRow; row++)
            {
                var record = TryReadRecord(worksheet.Row(row));
                if (record is not null
                    && record.Group == group
                    && record.LastName == student.LastName
                    && record.FirstName == student.FirstName)
                {
                    return record;
                }
            }

            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new JournalUnavailableException(_filePath, exception);
        }
    }

    public void Append(ResultRecord record)
    {
        try
        {
            using var workbook = File.Exists(_filePath) ? new XLWorkbook(_filePath) : CreateWorkbook();
            var worksheet = workbook.Worksheets.First();
            var row = worksheet.Row((worksheet.LastRowUsed()?.RowNumber() ?? 0) + 1);

            row.Cell(1).Value = record.Group;
            row.Cell(2).Value = record.LastName;
            row.Cell(3).Value = record.FirstName;
            row.Cell(4).Value = record.TicketNumber;
            row.Cell(5).Value = record.GeneratedAt;
            row.Cell(5).Style.DateFormat.Format = DateTimeFormat;
            row.Cell(6).Value = record.IsRepeat ? RepeatYes : RepeatNo;

            Save(workbook);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new JournalUnavailableException(_filePath, exception);
        }
    }

    private static ResultRecord? TryReadRecord(IXLRow row)
    {
        if (!row.Cell(4).TryGetValue<int>(out var ticketNumber)
            || !row.Cell(5).TryGetValue<DateTime>(out var generatedAt))
        {
            return null;
        }

        return new ResultRecord(
            row.Cell(1).GetFormattedString().Trim(),
            row.Cell(2).GetFormattedString().Trim(),
            row.Cell(3).GetFormattedString().Trim(),
            ticketNumber,
            generatedAt,
            row.Cell(6).GetFormattedString().Trim() == RepeatYes);
    }

    private static XLWorkbook CreateWorkbook()
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(WorksheetName);

        for (var i = 0; i < Headers.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = Headers[i];
        }

        worksheet.Row(1).Style.Font.Bold = true;
        return workbook;
    }

    // Saves to a temporary file first so that a crash during saving cannot corrupt the journal.
    private void Save(XLWorkbook workbook)
    {
        var tempPath = Path.ChangeExtension(_filePath, ".tmp.xlsx");

        try
        {
            workbook.SaveAs(tempPath);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
