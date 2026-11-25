using ExamTickets.Core.Models;

namespace ExamTickets.Core.Abstractions;

public interface ITicketAssigner
{
    TicketAssignment Assign(string group, Student student);
}
