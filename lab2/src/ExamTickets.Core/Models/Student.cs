namespace ExamTickets.Core.Models;

public sealed record Student(string LastName, string FirstName)
{
    public string FullName => $"{LastName} {FirstName}";
}
