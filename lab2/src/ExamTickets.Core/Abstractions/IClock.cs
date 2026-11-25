namespace ExamTickets.Core.Abstractions;

public interface IClock
{
    DateTime Now { get; }
}
