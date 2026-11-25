using ExamTickets.Core.Models;

namespace ExamTickets.Core.Abstractions;

public interface IStudentSource
{
    IReadOnlyList<string> GetGroups();

    IReadOnlyList<Student> GetStudents(string group);
}
