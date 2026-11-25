using ExamTickets.Presentation.ViewModels;

namespace ExamTickets.Presentation.Abstractions;

public interface IResultPresenter
{
    void Show(ResultViewModel result);
}
