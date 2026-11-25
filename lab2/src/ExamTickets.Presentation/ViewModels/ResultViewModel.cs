using ExamTickets.Core.Models;

namespace ExamTickets.Presentation.ViewModels;

public sealed class ResultViewModel
{
    public ResultViewModel(TicketAssignment assignment)
    {
        var record = assignment.Record;

        Title = $"Ваш билет № {assignment.Ticket.Number}";
        StudentName = $"{record.LastName} {record.FirstName}, группа {record.Group}";
        Questions = assignment.Ticket.Questions
            .Select((question, index) => $"{index + 1}. {question}")
            .ToList();
    }

    public string Title { get; }

    public string StudentName { get; }

    public IReadOnlyList<string> Questions { get; }
}
