using ExamTickets.Core.Abstractions;

namespace ExamTickets.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
