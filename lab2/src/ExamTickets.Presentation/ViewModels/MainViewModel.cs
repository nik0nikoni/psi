using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;
using ExamTickets.Presentation.Abstractions;
using ExamTickets.Presentation.Mvvm;

namespace ExamTickets.Presentation.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly IStudentSource _studentSource;
    private readonly ITicketAssigner _ticketAssigner;
    private readonly IResultPresenter _resultPresenter;
    private readonly IUserNotifier _notifier;
    private readonly RelayCommand _generateCommand;

    private IReadOnlyList<string> _groups = [];
    private IReadOnlyList<Student> _students = [];
    private string? _selectedGroup;
    private Student? _selectedStudent;

    public MainViewModel(
        IStudentSource studentSource,
        ITicketAssigner ticketAssigner,
        IResultPresenter resultPresenter,
        IUserNotifier notifier)
    {
        _studentSource = studentSource;
        _ticketAssigner = ticketAssigner;
        _resultPresenter = resultPresenter;
        _notifier = notifier;
        _generateCommand = new RelayCommand(Generate, CanGenerate);
    }

    public IReadOnlyList<string> Groups
    {
        get => _groups;
        private set => SetProperty(ref _groups, value);
    }

    // The view shows each student by Student.FullName, i.e. "LastName FirstName".
    public IReadOnlyList<Student> Students
    {
        get => _students;
        private set => SetProperty(ref _students, value);
    }

    public string? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (!SetProperty(ref _selectedGroup, value))
            {
                return;
            }

            SelectedStudent = null;
            Students = value is null ? [] : LoadStudents(value);
            _generateCommand.RaiseCanExecuteChanged();
        }
    }

    public Student? SelectedStudent
    {
        get => _selectedStudent;
        set
        {
            if (SetProperty(ref _selectedStudent, value))
            {
                _generateCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand GenerateCommand => _generateCommand;

    // Returns false when the student list cannot be loaded, so the caller can stop the application.
    public bool Initialize()
    {
        try
        {
            Groups = _studentSource.GetGroups();
            return true;
        }
        catch (SourceFileMissingException exception)
        {
            _notifier.ShowError(exception.Message);
            return false;
        }
    }

    private IReadOnlyList<Student> LoadStudents(string group)
    {
        try
        {
            return _studentSource.GetStudents(group);
        }
        catch (SourceFileMissingException exception)
        {
            _notifier.ShowError(exception.Message);
            return [];
        }
    }

    private bool CanGenerate() => SelectedGroup is not null && SelectedStudent is not null;

    private void Generate()
    {
        var group = SelectedGroup!;
        var student = SelectedStudent!;

        while (true)
        {
            try
            {
                var assignment = _ticketAssigner.Assign(group, student);
                _resultPresenter.Show(new ResultViewModel(assignment));
                return;
            }
            catch (JournalUnavailableException exception)
            {
                if (!_notifier.AskRetry(exception.Message))
                {
                    return;
                }
            }
            catch (TicketNotFoundException exception)
            {
                _notifier.ShowError(exception.Message);
                return;
            }
        }
    }
}
