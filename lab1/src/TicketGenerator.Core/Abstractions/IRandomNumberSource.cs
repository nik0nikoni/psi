namespace TicketGenerator.Core.Abstractions;

public interface IRandomNumberSource
{
    int Next(int minInclusive, int maxExclusive);
}
