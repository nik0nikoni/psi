namespace TicketGenerator.Core.Abstractions;

public interface IClock
{
    DateTime Now { get; }
}
