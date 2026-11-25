namespace ExamTickets.Core.Models;

public sealed record Ticket(int Number, IReadOnlyList<string> Questions);
