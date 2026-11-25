using TicketGenerator.Core.Abstractions;

namespace TicketGenerator.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
