using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;

namespace ExamTickets.Infrastructure.Word;

public sealed class DocxTicketSource : ITicketSource
{
    private readonly string _filePath;

    public DocxTicketSource(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<string> ReadParagraphs()
    {
        if (!File.Exists(_filePath))
        {
            throw new SourceFileMissingException(_filePath);
        }

        // FileShare.ReadWrite lets the document be read while it is open in Word.
        using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var document = WordprocessingDocument.Open(stream, isEditable: false);

        var body = document.MainDocumentPart?.Document?.Body;
        if (body is null)
        {
            return [];
        }

        var paragraphs = new List<string>();
        var listItemNumber = 0;

        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var text = string.Concat(paragraph.Descendants<Text>().Select(part => part.Text));

            // Word stores automatic list numbers outside the text, so "1. " is restored here.
            if (IsNumberedListItem(paragraph))
            {
                listItemNumber++;
                text = $"{listItemNumber}. {text}";
            }
            else
            {
                listItemNumber = 0;
            }

            paragraphs.Add(text);
        }

        return paragraphs;
    }

    private static bool IsNumberedListItem(Paragraph paragraph)
    {
        var numberingId = paragraph.ParagraphProperties?.NumberingProperties?.NumberingId?.Val;
        return numberingId is not null && numberingId.Value != 0;
    }
}
