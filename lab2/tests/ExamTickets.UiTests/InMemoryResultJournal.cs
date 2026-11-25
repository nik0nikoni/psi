using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Models;

namespace ExamTickets.UiTests;

internal sealed class InMemoryResultJournal : IResultJournal
{
    public List<ResultRecord> Records { get; } = [];

    public ResultRecord? FindFirst(string group, Student student) =>
        Records.FirstOrDefault(record =>
            record.Group == group
            && record.LastName == student.LastName
            && record.FirstName == student.FirstName);

    public void Append(ResultRecord record) => Records.Add(record);
}
