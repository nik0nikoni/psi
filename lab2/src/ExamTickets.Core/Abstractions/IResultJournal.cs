using ExamTickets.Core.Models;

namespace ExamTickets.Core.Abstractions;

public interface IResultJournal
{
    ResultRecord? FindFirst(string group, Student student);

    void Append(ResultRecord record);
}
