using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Parsing;
using ExamTickets.Infrastructure.Word;
using NSubstitute;

namespace ExamTickets.UnitTests.Word;

public sealed class DocxTicketSourceTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public DocxTicketSourceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "exam-tickets-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "tickets.docx");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void ReadParagraphs_PlainParagraphs_ReturnsTextInDocumentOrder()
    {
        CreateDocument(
            PlainParagraph("Билет 1"),
            PlainParagraph("1. Вопрос первый"),
            PlainParagraph(""),
            PlainParagraph("2. Вопрос второй"));

        var paragraphs = new DocxTicketSource(_filePath).ReadParagraphs();

        Assert.Equal(["Билет 1", "1. Вопрос первый", "", "2. Вопрос второй"], paragraphs);
    }

    [Fact]
    public void ReadParagraphs_ParagraphSplitIntoRuns_JoinsRuns()
    {
        CreateDocument(new Paragraph(
            new Run(new Text("Билет ")),
            new Run(new RunProperties(new Bold()), new Text("12"))));

        var paragraphs = new DocxTicketSource(_filePath).ReadParagraphs();

        Assert.Equal(["Билет 12"], paragraphs);
    }

    [Fact]
    public void ReadParagraphs_WordNumberedList_RestoresQuestionNumbers()
    {
        CreateDocument(
            PlainParagraph("Билет 1"),
            ListParagraph("Вопрос первый"),
            ListParagraph("Вопрос второй"),
            PlainParagraph("Билет 2"),
            ListParagraph("Вопрос третий"));

        var paragraphs = new DocxTicketSource(_filePath).ReadParagraphs();

        Assert.Equal(
            ["Билет 1", "1. Вопрос первый", "2. Вопрос второй", "Билет 2", "1. Вопрос третий"],
            paragraphs);
    }

    [Fact]
    public void ReadParagraphs_FileOpenedByAnotherProcess_StillReads()
    {
        CreateDocument(PlainParagraph("Билет 1"));
        using var openedElsewhere = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

        var paragraphs = new DocxTicketSource(_filePath).ReadParagraphs();

        Assert.Equal(["Билет 1"], paragraphs);
    }

    [Fact]
    public void ReadParagraphs_MissingFile_ThrowsSourceFileMissingException()
    {
        var source = new DocxTicketSource(_filePath);

        var exception = Assert.Throws<SourceFileMissingException>(() => source.ReadParagraphs());

        Assert.Equal(_filePath, exception.FilePath);
        Assert.Contains(_filePath, exception.Message);
    }

    [Fact]
    public void ReadParagraphs_ParsedByTicketParser_ReturnsTicketFromWord()
    {
        CreateDocument(
            PlainParagraph("Билет 3"),
            ListParagraph("Вопрос первый"),
            ListParagraph("Вопрос второй"),
            ListParagraph("Вопрос третий"));
        var parser = new TicketParser(Substitute.For<IAppLogger>());

        var tickets = parser.Parse(new DocxTicketSource(_filePath).ReadParagraphs());

        var ticket = Assert.Single(tickets);
        Assert.Equal(3, ticket.Number);
        Assert.Equal(["Вопрос первый", "Вопрос второй", "Вопрос третий"], ticket.Questions);
    }

    private void CreateDocument(params Paragraph[] paragraphs)
    {
        using var document = WordprocessingDocument.Create(_filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document(new Body(paragraphs));
    }

    private static Paragraph PlainParagraph(string text) => new(new Run(new Text(text)));

    private static Paragraph ListParagraph(string text) =>
        new(
            new ParagraphProperties(
                new NumberingProperties(
                    new NumberingLevelReference { Val = 0 },
                    new NumberingId { Val = 1 })),
            new Run(new Text(text)));
}
