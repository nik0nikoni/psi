using System.Windows;
using ExamTickets.Presentation.Abstractions;
using ExamTickets.Presentation.ViewModels;
using ExamTickets.Wpf.Views;

namespace ExamTickets.Wpf.Services;

public sealed class ResultWindowPresenter : IResultPresenter
{
    private readonly Window _owner;

    public ResultWindowPresenter(Window owner)
    {
        _owner = owner;
    }

    public void Show(ResultViewModel result)
    {
        var window = new ResultWindow
        {
            DataContext = result,
            Owner = _owner,
        };

        window.ShowDialog();
    }
}
