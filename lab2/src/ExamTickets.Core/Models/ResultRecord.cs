namespace ExamTickets.Core.Models;

public sealed record ResultRecord(
    string Group,
    string LastName,
    string FirstName,
    int TicketNumber,
    DateTime GeneratedAt,
    bool IsRepeat);
