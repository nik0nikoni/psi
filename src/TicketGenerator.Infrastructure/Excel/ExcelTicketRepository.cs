using ClosedXML.Excel;
using TicketGenerator.Core.Abstractions;
using TicketGenerator.Core.Exceptions;
using TicketGenerator.Core.Models;

namespace TicketGenerator.Infrastructure.Excel;

public sealed class ExcelTicketRepository : ITicketRepository
{
    public const string WorksheetName = "Journal";
    public const string DateTimeFormat = "dd.MM.yyyy HH:mm:ss";

    public static readonly IReadOnlyList<string> Headers =
        ["Last name", "First name", "Номер билета", "Дата и время"];

    private readonly string _filePath;

    public ExcelTicketRepository(string filePath)
    {
        _filePath = Path.GetFullPath(filePath);
    }

    public void Append(TicketRecord record)
    {
        try
        {
            using var workbook = File.Exists(_filePath) ? new XLWorkbook(_filePath) : CreateWorkbook();
            var worksheet = workbook.Worksheets.First();
            var row = worksheet.Row((worksheet.LastRowUsed()?.RowNumber() ?? 0) + 1);

            row.Cell(1).Value = record.LastName;
            row.Cell(2).Value = record.FirstName;
            row.Cell(3).Value = record.TicketNumber;
            row.Cell(4).Value = record.IssuedAt;
            row.Cell(4).Style.DateFormat.Format = DateTimeFormat;

            Save(workbook);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new JournalUnavailableException(_filePath, exception);
        }
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
