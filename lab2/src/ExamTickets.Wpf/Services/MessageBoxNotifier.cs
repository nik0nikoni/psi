using System.Windows;
using ExamTickets.Presentation.Abstractions;

namespace ExamTickets.Wpf.Services;

public sealed class MessageBoxNotifier : IUserNotifier
{
    private const string Caption = "Генератор билетов";

    public void ShowError(string message)
    {
        Show(message, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool AskRetry(string message)
    {
        var answer = Show(
            message + Environment.NewLine + Environment.NewLine + "Повторить попытку?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return answer == MessageBoxResult.Yes;
    }

    // Messages are owned by the main window once it is visible, so they stay on top of it.
    private static MessageBoxResult Show(string message, MessageBoxButton buttons, MessageBoxImage image)
    {
        var owner = Application.Current?.MainWindow;

        return owner is { IsVisible: true }
            ? MessageBox.Show(owner, message, Caption, buttons, image)
            : MessageBox.Show(message, Caption, buttons, image);
    }
}
