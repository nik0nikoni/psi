using System.Windows;
using System.Windows.Input;

namespace ExamTickets.Wpf.Views;

public partial class ResultWindow : Window
{
    public ResultWindow()
    {
        InitializeComponent();
    }

    // Esc closes only this window; the main window stays open for the next student.
    private void OnCloseExecuted(object sender, ExecutedRoutedEventArgs e) => Close();
}
