namespace ExamTickets.Core.Exceptions;

public sealed class NoValidTicketsException : Exception
{
    public NoValidTicketsException()
        : base("В файле билетов нет ни одного корректного билета. Проверьте tickets.docx и журнал ошибок.")
    {
    }
}
