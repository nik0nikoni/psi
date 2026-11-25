using ExamTickets.Core.Abstractions;

namespace ExamTickets.Infrastructure.Randomness;

public sealed class SystemRandomNumberSource : IRandomNumberSource
{
    public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);
}
