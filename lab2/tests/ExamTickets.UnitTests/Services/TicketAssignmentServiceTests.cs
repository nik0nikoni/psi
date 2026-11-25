using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;
using ExamTickets.Core.Services;
using NSubstitute;

namespace ExamTickets.UnitTests.Services;

public sealed class TicketAssignmentServiceTests
{
    private const string Group = "TI-221";

    private static readonly DateTime Now = new(2026, 11, 25, 10, 0, 0);
    private static readonly Student Ivanov = new("Иванов", "Иван");
    private static readonly Student Petrov = new("Петров", "Пётр");

    private static readonly IReadOnlyList<Ticket> Tickets =
    [
        CreateTicket(1),
        CreateTicket(2),
        CreateTicket(3),
    ];

    private readonly IResultJournal _journal = Substitute.For<IResultJournal>();
    private readonly IRandomNumberSource _randomSource = Substitute.For<IRandomNumberSource>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public TicketAssignmentServiceTests()
    {
        _clock.Now.Returns(Now);
    }

    [Fact]
    public void Assign_NewStudent_PicksRandomTicketFromOneToN()
    {
        _randomSource.Next(1, 4).Returns(2);

        var assignment = CreateService().Assign(Group, Ivanov);

        Assert.Equal(Tickets[1], assignment.Ticket);
        Assert.Equal(new ResultRecord(Group, "Иванов", "Иван", 2, Now, IsRepeat: false), assignment.Record);
    }

    [Fact]
    public void Assign_NewStudent_SavesRecordImmediately()
    {
        _randomSource.Next(1, 4).Returns(3);

        var assignment = CreateService().Assign(Group, Ivanov);

        _journal.Received(1).Append(assignment.Record);
    }

    [Fact]
    public void Assign_StudentInJournal_ReusesFirstTicketAndMarksRepeat()
    {
        _journal.FindFirst(Group, Ivanov)
            .Returns(new ResultRecord(Group, "Иванов", "Иван", 3, Now.AddDays(-1), IsRepeat: false));

        var assignment = CreateService().Assign(Group, Ivanov);

        Assert.Equal(Tickets[2], assignment.Ticket);
        Assert.Equal(new ResultRecord(Group, "Иванов", "Иван", 3, Now, IsRepeat: true), assignment.Record);
        _journal.Received(1).Append(assignment.Record);
        _randomSource.DidNotReceiveWithAnyArgs().Next(default, default);
    }

    [Fact]
    public void Assign_SameStudentTwice_ReturnsSameTicketAndAppendsTwoRows()
    {
        var journal = new InMemoryResultJournal();
        _randomSource.Next(1, 4).Returns(2, 1);
        var service = new TicketAssignmentService(Tickets, journal, _randomSource, _clock);

        var first = service.Assign(Group, Ivanov);
        var second = service.Assign(Group, Ivanov);

        Assert.Equal(first.Ticket, second.Ticket);
        Assert.Equal([false, true], journal.Records.Select(record => record.IsRepeat));
        Assert.Equal([2, 2], journal.Records.Select(record => record.TicketNumber));
    }

    [Fact]
    public void Assign_DifferentStudents_AreAssignedIndependently()
    {
        var journal = new InMemoryResultJournal();
        _randomSource.Next(1, 4).Returns(2, 3);
        var service = new TicketAssignmentService(Tickets, journal, _randomSource, _clock);

        var ivanov = service.Assign(Group, Ivanov);
        var petrov = service.Assign(Group, Petrov);

        Assert.Equal(2, ivanov.Ticket.Number);
        Assert.Equal(3, petrov.Ticket.Number);
        Assert.All(journal.Records, record => Assert.False(record.IsRepeat));
    }

    [Fact]
    public void Assign_TicketNumbersWithGaps_UsesPositionInList()
    {
        IReadOnlyList<Ticket> tickets = [CreateTicket(1), CreateTicket(4), CreateTicket(9)];
        _randomSource.Next(1, 4).Returns(3);

        var assignment = new TicketAssignmentService(tickets, _journal, _randomSource, _clock).Assign(Group, Ivanov);

        Assert.Equal(9, assignment.Record.TicketNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Assign_RandomSourceOutOfRange_Throws(int position)
    {
        _randomSource.Next(1, 4).Returns(position);

        Assert.Throws<InvalidOperationException>(() => CreateService().Assign(Group, Ivanov));
        _journal.DidNotReceiveWithAnyArgs().Append(default!);
    }

    [Fact]
    public void Assign_JournalNumberMissingFromTickets_ThrowsTicketNotFoundException()
    {
        _journal.FindFirst(Group, Ivanov)
            .Returns(new ResultRecord(Group, "Иванов", "Иван", 42, Now, IsRepeat: false));

        var exception = Assert.Throws<TicketNotFoundException>(() => CreateService().Assign(Group, Ivanov));

        Assert.Equal(42, exception.TicketNumber);
        _journal.DidNotReceiveWithAnyArgs().Append(default!);
    }

    [Fact]
    public void Assign_JournalUnavailable_PropagatesDomainException()
    {
        _randomSource.Next(1, 4).Returns(1);
        _journal.When(journal => journal.Append(Arg.Any<ResultRecord>()))
            .Throw(new JournalUnavailableException("results.xlsx", new IOException()));

        Assert.Throws<JournalUnavailableException>(() => CreateService().Assign(Group, Ivanov));
    }

    [Fact]
    public void Constructor_NoTickets_ThrowsNoValidTicketsException()
    {
        Assert.Throws<NoValidTicketsException>(
            () => new TicketAssignmentService([], _journal, _randomSource, _clock));
    }

    private TicketAssignmentService CreateService() => new(Tickets, _journal, _randomSource, _clock);

    private static Ticket CreateTicket(int number) =>
        new(number, [$"Вопрос {number}.1", $"Вопрос {number}.2", $"Вопрос {number}.3"]);

    private sealed class InMemoryResultJournal : IResultJournal
    {
        public List<ResultRecord> Records { get; } = [];

        public ResultRecord? FindFirst(string group, Student student) =>
            Records.FirstOrDefault(record =>
                record.Group == group
                && record.LastName == student.LastName
                && record.FirstName == student.FirstName);

        public void Append(ResultRecord record) => Records.Add(record);
    }
}
