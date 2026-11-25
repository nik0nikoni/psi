namespace ExamTickets.Core.Abstractions;

public interface ITicketSource
{
    IReadOnlyList<string> ReadParagraphs();
}
