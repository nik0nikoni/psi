namespace ExamTickets.Core.Exceptions;

public sealed class TicketNotFoundException : Exception
{
    public TicketNotFoundException(int ticketNumber)
        : base(
            $"Билет № {ticketNumber}, ранее назначенный студенту, не найден в файле билетов. " +
            "Проверьте tickets.docx.")
    {
        TicketNumber = ticketNumber;
    }

    public int TicketNumber { get; }
}
