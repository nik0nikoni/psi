using TicketGenerator.Core.Abstractions;

namespace TicketGenerator.Infrastructure.Randomness;

public sealed class SystemRandomNumberSource : IRandomNumberSource
{
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
}
