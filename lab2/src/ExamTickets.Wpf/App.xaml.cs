using System.Windows;
using System.Windows.Threading;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Parsing;
using ExamTickets.Core.Services;
using ExamTickets.Infrastructure.Configuration;
using ExamTickets.Infrastructure.Excel;
using ExamTickets.Infrastructure.Logging;
using ExamTickets.Infrastructure.Randomness;
using ExamTickets.Infrastructure.Time;
using ExamTickets.Infrastructure.Word;
using ExamTickets.Presentation.Abstractions;
using ExamTickets.Presentation.ViewModels;
using ExamTickets.Wpf.Services;
using ExamTickets.Wpf.Views;

namespace ExamTickets.Wpf;

public partial class App : Application
{
    private readonly IUserNotifier _notifier = new MessageBoxNotifier();
    private IAppLogger? _logger;

    // Composition root: dependencies are wired here by hand instead of StartupUri.
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var paths = DataPaths.FromArguments(e.Args, Environment.CurrentDirectory);
        var clock = new SystemClock();
        _logger = new FileAppLogger(paths.LogFile, clock);

        try
        {
            var tickets = new TicketParser(_logger).Parse(new DocxTicketSource(paths.TicketsFile).ReadParagraphs());
            var journal = new ExcelResultJournal(paths.ResultsFile);
            journal.EnsureCreated();
            var assigner = new TicketAssignmentService(tickets, journal, new SystemRandomNumberSource(), clock);

            var mainWindow = new MainWindow();
            var viewModel = new MainViewModel(
                new ExcelStudentSource(paths.StudentsFile),
                assigner,
                new ResultWindowPresenter(mainWindow),
                _notifier);

            if (!viewModel.Initialize())
            {
                Shutdown(1);
                return;
            }

            mainWindow.DataContext = viewModel;
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception) when (exception is SourceFileMissingException
                                              or NoValidTicketsException
                                              or JournalUnavailableException)
        {
            _logger.Error(exception.Message);
            _notifier.ShowError(exception.Message);
            Shutdown(1);
        }
    }

    // Last line of defence: an unexpected error (e.g. a damaged file) is reported instead of crashing.
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.Error(e.Exception.ToString());
        _notifier.ShowError($"Непредвиденная ошибка: {e.Exception.Message}");
        e.Handled = true;

        if (MainWindow is null || !MainWindow.IsVisible)
        {
            Shutdown(1);
        }
    }
}
