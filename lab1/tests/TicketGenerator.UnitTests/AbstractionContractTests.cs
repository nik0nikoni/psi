using NSubstitute;
using TicketGenerator.Core.Abstractions;
using TicketGenerator.Core.Models;

namespace TicketGenerator.UnitTests;

public sealed class AbstractionContractTests
{
    [Fact]
    public void Repository_AppendsATicketRecord()
    {
        var repository = Substitute.For<ITicketRepository>();
        var record = new TicketRecord(
            "Иванов",
            "Иван",
            7,
            new DateTime(2026, 10, 28, 12, 30, 0, DateTimeKind.Utc));

        repository.Append(record);

        repository.Received(1).Append(record);
    }

    [Fact]
    public void RandomSource_ReceivesExclusiveUpperBound()
    {
        var randomSource = Substitute.For<IRandomNumberSource>();
        randomSource.Next(1, 21).Returns(7);

        var number = randomSource.Next(1, 21);

        Assert.Equal(7, number);
        randomSource.Received(1).Next(1, 21);
    }

    [Fact]
    public void Clock_ProvidesTheCurrentTime()
    {
        var now = new DateTime(2026, 10, 28, 12, 30, 0, DateTimeKind.Utc);
        var clock = Substitute.For<IClock>();
        clock.Now.Returns(now);

        Assert.Equal(now, clock.Now);
    }
}
