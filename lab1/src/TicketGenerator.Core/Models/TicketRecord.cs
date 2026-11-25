namespace TicketGenerator.Core.Models;

public sealed record TicketRecord(
    string LastName,
    string FirstName,
    int TicketNumber,
    DateTime IssuedAt);
