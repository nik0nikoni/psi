using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;

namespace ExamTickets.Core.Services;

public sealed class TicketAssignmentService : ITicketAssigner
{
    private readonly IReadOnlyList<Ticket> _tickets;
    private readonly IResultJournal _journal;
    private readonly IRandomNumberSource _randomSource;
    private readonly IClock _clock;

    public TicketAssignmentService(
        IReadOnlyList<Ticket> tickets,
        IResultJournal journal,
        IRandomNumberSource randomSource,
        IClock clock)
    {
        if (tickets.Count == 0)
        {
            throw new NoValidTicketsException();
        }

        _tickets = tickets;
        _journal = journal;
        _randomSource = randomSource;
        _clock = clock;
    }

    public TicketAssignment Assign(string group, Student student)
    {
        var firstRecord = _journal.FindFirst(group, student);
        var ticket = firstRecord is null ? PickRandomTicket() : FindTicket(firstRecord.TicketNumber);

        var record = new ResultRecord(
            group,
            student.LastName,
            student.FirstName,
            ticket.Number,
            _clock.Now,
            IsRepeat: firstRecord is not null);

        // Saved right away, so the journal never depends on how the application is closed.
        _journal.Append(record);
        return new TicketAssignment(ticket, record);
    }

    // Draws a position 1..N in the list, so gaps left by skipped tickets do not matter.
    private Ticket PickRandomTicket()
    {
        var position = _randomSource.Next(1, _tickets.Count + 1);

        if (position < 1 || position > _tickets.Count)
        {
            throw new InvalidOperationException(
                $"Random source returned {position}, expected a value from 1 to {_tickets.Count}.");
        }

        return _tickets[position - 1];
    }

    private Ticket FindTicket(int number) =>
        _tickets.FirstOrDefault(ticket => ticket.Number == number)
        ?? throw new TicketNotFoundException(number);
}
