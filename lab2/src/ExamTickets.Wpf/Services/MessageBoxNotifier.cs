using System.Windows;
using ExamTickets.Presentation.Abstractions;

namespace ExamTickets.Wpf.Services;

public sealed class MessageBoxNotifier : IUserNotifier
{
    private const string Caption = "Генератор билетов";

    public void ShowError(string message)
    {
        MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool AskRetry(string message)
    {
        var answer = MessageBox.Show(
            message + Environment.NewLine + Environment.NewLine + "Повторить попытку?",
            Caption,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return answer == MessageBoxResult.Yes;
    }
}
