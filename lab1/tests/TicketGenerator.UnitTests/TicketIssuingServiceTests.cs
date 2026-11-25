using NSubstitute;
using TicketGenerator.Core.Abstractions;
using TicketGenerator.Core.Exceptions;
using TicketGenerator.Core.Generation;
using TicketGenerator.Core.Models;
using TicketGenerator.Core.Services;
using TicketGenerator.Core.Validation;

namespace TicketGenerator.UnitTests;

public sealed class TicketIssuingServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 30, 0);

    private readonly IRandomNumberSource _randomSource = Substitute.For<IRandomNumberSource>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ITicketRepository _repository = Substitute.For<ITicketRepository>();
    private readonly TicketIssuingService _service;

    public TicketIssuingServiceTests()
    {
        _randomSource.Next(1, 21).Returns(7);
        _clock.Now.Returns(Now);
        _service = new TicketIssuingService(
            new StudentNameValidator(),
            new TicketNumberGenerator(_randomSource),
            _clock,
            _repository);
    }

    [Fact]
    public void Issue_ValidNames_ReturnsRecordWithTicketAndTime()
    {
        var record = _service.Issue("Иванов", "Иван");

        Assert.Equal(new TicketRecord("Иванов", "Иван", 7, Now), record);
    }

    [Fact]
    public void Issue_ValidNames_SavesRecordOnce()
    {
        var record = _service.Issue("Иванов", "Иван");

        _repository.Received(1).Append(record);
    }

    [Fact]
    public void Issue_NamesWithSurroundingSpaces_AreTrimmed()
    {
        var record = _service.Issue("  Иванов ", " Иван  ");

        Assert.Equal("Иванов", record.LastName);
        Assert.Equal("Иван", record.FirstName);
    }

    [Theory]
    [InlineData("", "Иван")]
    [InlineData("Иванов", "   ")]
    public void Issue_EmptyName_ThrowsAndDoesNotSave(string lastName, string firstName)
    {
        Assert.Throws<ArgumentException>(() => _service.Issue(lastName, firstName));

        _repository.DidNotReceiveWithAnyArgs().Append(default!);
    }

    [Fact]
    public void Issue_RepositoryUnavailable_PropagatesDomainException()
    {
        _repository
            .When(r => r.Append(Arg.Any<TicketRecord>()))
            .Throw(new JournalUnavailableException("journal.xlsx", new IOException()));

        Assert.Throws<JournalUnavailableException>(() => _service.Issue("Иванов", "Иван"));
    }
}
