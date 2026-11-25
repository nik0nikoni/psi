using ExamTickets.Core.Models;
using ExamTickets.Presentation.ViewModels;

namespace ExamTickets.UiTests;

public sealed class ResultViewModelTests
{
    [Fact]
    public void Criterion3_ShowsTicketNumberAndThreeQuestions()
    {
        var ticket = new Ticket(5, ["Первый", "Второй", "Третий"]);
        var record = new ResultRecord("TI-221", "Иванов", "Иван", 5, DateTime.Now, IsRepeat: false);

        var result = new ResultViewModel(new TicketAssignment(ticket, record));

        Assert.Equal("Ваш билет № 5", result.Title);
        Assert.Equal(["1. Первый", "2. Второй", "3. Третий"], result.Questions);
        Assert.Equal("Иванов Иван, группа TI-221", result.StudentName);
    }
}
