using TicketGenerator.Core.Abstractions;

namespace TicketGenerator.Core.Generation;

public sealed class TicketNumberGenerator
{
    public const int MinTicketNumber = 1;
    public const int MaxTicketNumber = 20;

    private readonly IRandomNumberSource _randomSource;

    public TicketNumberGenerator(IRandomNumberSource randomSource)
    {
        _randomSource = randomSource;
    }

    public int Generate()
    {
        var number = _randomSource.Next(MinTicketNumber, MaxTicketNumber + 1);

        if (number < MinTicketNumber || number > MaxTicketNumber)
        {
            throw new InvalidOperationException(
                $"Random source returned {number}, expected a value from {MinTicketNumber} to {MaxTicketNumber}.");
        }

        return number;
    }
}
