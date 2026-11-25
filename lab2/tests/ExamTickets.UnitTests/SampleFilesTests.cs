using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Parsing;
using ExamTickets.Infrastructure.Excel;
using ExamTickets.Infrastructure.Word;
using NSubstitute;

namespace ExamTickets.UnitTests;

public sealed class SampleFilesTests
{
    private static readonly string SamplesDirectory = Path.Combine(AppContext.BaseDirectory, "samples");
    private static readonly string StudentsFile = Path.Combine(SamplesDirectory, "students.xlsx");
    private static readonly string TicketsFile = Path.Combine(SamplesDirectory, "tickets.docx");

    [Fact]
    public void Students_ContainSeveralGroupsIncludingEmptyOne()
    {
        var source = new ExcelStudentSource(StudentsFile);

        Assert.Equal(["TI-221", "TI-222", "FAF-231", "Пустая группа"], source.GetGroups());
        Assert.Equal(5, source.GetStudents("TI-221").Count);
        Assert.Empty(source.GetStudents("Пустая группа"));
    }

    [Fact]
    public void Tickets_InvalidOnesAreSkippedAndLogged()
    {
        var logger = Substitute.For<IAppLogger>();

        var tickets = new TicketParser(logger).Parse(new DocxTicketSource(TicketsFile).ReadParagraphs());

        Assert.Equal([1, 2, 3, 4, 5, 8, 9, 10], tickets.Select(ticket => ticket.Number));
        Assert.All(tickets, ticket => Assert.Equal(3, ticket.Questions.Count));
        logger.Received(1).Error(Arg.Is<string>(message => message.Contains("Билет 6")));
        logger.Received(1).Error(Arg.Is<string>(message => message.Contains("Билет VII")));
    }

    [Fact]
    public void Tickets_IsValidWordDocument()
    {
        using var document = WordprocessingDocument.Open(TicketsFile, isEditable: false);

        var errors = new OpenXmlValidator().Validate(document).Select(error => error.Description);

        Assert.Empty(errors);
    }
}
