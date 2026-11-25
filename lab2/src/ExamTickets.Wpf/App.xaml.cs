using System.Windows;

namespace ExamTickets.Wpf;

public partial class App : Application
{
    // Composition root: dependencies are wired here by hand instead of StartupUri.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }
}
