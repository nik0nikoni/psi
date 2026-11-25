using TicketGenerator.Core.Models;

namespace TicketGenerator.UnitTests;

public sealed class TicketRecordTests
{
    [Fact]
    public void Constructor_PreservesStudentTicketAndIssueTime()
    {
        var issuedAt = new DateTime(2026, 10, 28, 12, 30, 0, DateTimeKind.Utc);

        var record = new TicketRecord("Иванов", "Иван", 7, issuedAt);

        Assert.Equal("Иванов", record.LastName);
        Assert.Equal("Иван", record.FirstName);
        Assert.Equal(7, record.TicketNumber);
        Assert.Equal(issuedAt, record.IssuedAt);
    }

    [Fact]
    public void EqualValues_ProduceEqualRecords()
    {
        var issuedAt = new DateTime(2026, 10, 28, 12, 30, 0, DateTimeKind.Utc);

        var first = new TicketRecord("Иванов", "Иван", 7, issuedAt);
        var second = new TicketRecord("Иванов", "Иван", 7, issuedAt);

        Assert.Equal(first, second);
    }
}
