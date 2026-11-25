using NSubstitute;
using TicketGenerator.Core.Abstractions;
using TicketGenerator.Core.Generation;

namespace TicketGenerator.UnitTests;

public sealed class TicketNumberGeneratorTests
{
    private readonly IRandomNumberSource _randomSource = Substitute.For<IRandomNumberSource>();

    [Fact]
    public void Generate_RequestsNumberFromOneToTwentyInclusive()
    {
        _randomSource.Next(Arg.Any<int>(), Arg.Any<int>()).Returns(5);
        var generator = new TicketNumberGenerator(_randomSource);

        generator.Generate();

        _randomSource.Received(1).Next(1, 21);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(20)]
    public void Generate_ReturnsValueFromRandomSource(int value)
    {
        _randomSource.Next(1, 21).Returns(value);
        var generator = new TicketNumberGenerator(_randomSource);

        var number = generator.Generate();

        Assert.Equal(value, number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Generate_OutOfRangeValue_Throws(int value)
    {
        _randomSource.Next(1, 21).Returns(value);
        var generator = new TicketNumberGenerator(_randomSource);

        Assert.Throws<InvalidOperationException>(() => generator.Generate());
    }

    [Fact]
    public void Generate_WithRealRandom_StaysWithinRange()
    {
        var generator = new TicketNumberGenerator(new RandomAdapter());

        for (var i = 0; i < 1000; i++)
        {
            Assert.InRange(generator.Generate(), 1, 20);
        }
    }

    private sealed class RandomAdapter : IRandomNumberSource
    {
        private readonly Random _random = new(42);

        public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
