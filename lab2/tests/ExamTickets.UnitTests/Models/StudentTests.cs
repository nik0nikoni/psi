using ExamTickets.Core.Models;

namespace ExamTickets.UnitTests.Models;

public class StudentTests
{
    [Fact]
    public void FullName_ReturnsLastNameThenFirstName()
    {
        var student = new Student("Иванов", "Иван");

        Assert.Equal("Иванов Иван", student.FullName);
    }
}
