using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Exceptions;
using ExamTickets.Core.Models;
using ExamTickets.Core.Services;
using ExamTickets.Presentation.Abstractions;
using ExamTickets.Presentation.ViewModels;
using NSubstitute;

namespace ExamTickets.UiTests;

public sealed class MainViewModelTests
{
    private const string Group221 = "TI-221";
    private const string Group222 = "TI-222";
    private const string EmptyGroup = "Пустая";

    private static readonly Student Ivanov = new("Иванов", "Иван");
    private static readonly Student Sidorova = new("Сидорова", "Анна");
    private static readonly Student Petrov = new("Петров", "Пётр");

    private static readonly IReadOnlyList<Ticket> Tickets =
    [
        new(1, ["A1", "A2", "A3"]),
        new(2, ["B1", "B2", "B3"]),
        new(3, ["C1", "C2", "C3"]),
    ];

    private readonly IStudentSource _studentSource = Substitute.For<IStudentSource>();
    private readonly IRandomNumberSource _randomSource = Substitute.For<IRandomNumberSource>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IResultPresenter _presenter = Substitute.For<IResultPresenter>();
    private readonly IUserNotifier _notifier = Substitute.For<IUserNotifier>();
    private readonly InMemoryResultJournal _journal = new();
    private readonly List<ResultViewModel> _shownResults = [];

    public MainViewModelTests()
    {
        _studentSource.GetGroups().Returns([Group221, Group222, EmptyGroup]);
        _studentSource.GetStudents(Group221).Returns([Ivanov, Sidorova]);
        _studentSource.GetStudents(Group222).Returns([Petrov]);
        _studentSource.GetStudents(EmptyGroup).Returns([]);
        _randomSource.Next(1, 4).Returns(2);

        var time = new DateTime(2026, 11, 25, 10, 0, 0);
        _clock.Now.Returns(_ => time = time.AddMinutes(1));

        _presenter.Show(Arg.Do<ResultViewModel>(_shownResults.Add));
    }

    [Fact]
    public void Initialize_FillsGroupsWithWorksheetNames()
    {
        var viewModel = CreateViewModel();

        Assert.True(viewModel.Initialize());

        Assert.Equal([Group221, Group222, EmptyGroup], viewModel.Groups);
        Assert.Empty(viewModel.Students);
    }

    [Fact]
    public void Criterion1_SelectingGroup_ShowsStudentsOfThatSheetAsLastNameFirstName()
    {
        var viewModel = CreateInitializedViewModel();

        viewModel.SelectedGroup = Group221;

        Assert.Equal(["Иванов Иван", "Сидорова Анна"], viewModel.Students.Select(student => student.FullName));
    }

    [Fact]
    public void Criterion1_ChangingGroup_ReplacesStudentsAndClearsSelection()
    {
        var viewModel = CreateInitializedViewModel();
        viewModel.SelectedGroup = Group221;
        viewModel.SelectedStudent = Ivanov;

        viewModel.SelectedGroup = Group222;

        Assert.Equal([Petrov], viewModel.Students);
        Assert.Null(viewModel.SelectedStudent);
    }

    [Fact]
    public void Criterion2_ButtonIsEnabledOnlyWhenGroupAndStudentAreSelected()
    {
        var viewModel = CreateInitializedViewModel();
        Assert.False(viewModel.GenerateCommand.CanExecute(null));

        viewModel.SelectedGroup = Group221;
        Assert.False(viewModel.GenerateCommand.CanExecute(null));

        viewModel.SelectedStudent = Ivanov;
        Assert.True(viewModel.GenerateCommand.CanExecute(null));

        viewModel.SelectedGroup = Group222;
        Assert.False(viewModel.GenerateCommand.CanExecute(null));
    }

    [Fact]
    public void Criterion2_SelectionChanges_RaiseCanExecuteChanged()
    {
        var viewModel = CreateInitializedViewModel();
        var raised = 0;
        viewModel.GenerateCommand.CanExecuteChanged += (_, _) => raised++;

        viewModel.SelectedGroup = Group221;
        viewModel.SelectedStudent = Ivanov;

        Assert.True(raised >= 2);
    }

    [Fact]
    public void EmptyGroup_ShowsEmptyListAndKeepsButtonDisabled()
    {
        var viewModel = CreateInitializedViewModel();

        viewModel.SelectedGroup = EmptyGroup;

        Assert.Empty(viewModel.Students);
        Assert.False(viewModel.GenerateCommand.CanExecute(null));
        _notifier.DidNotReceiveWithAnyArgs().ShowError(default!);
    }

    [Fact]
    public void Criterion3_Generate_ShowsTicketWithThreeQuestions()
    {
        var viewModel = CreateSelectedViewModel(Group221, Ivanov);

        viewModel.GenerateCommand.Execute(null);

        var result = Assert.Single(_shownResults);
        Assert.Equal("Ваш билет № 2", result.Title);
        Assert.Equal(["1. B1", "2. B2", "3. B3"], result.Questions);
    }

    [Fact]
    public void Criterion4_SameStudentAgain_GetsSameTicketNumber()
    {
        _randomSource.Next(1, 4).Returns(2, 3);
        var viewModel = CreateSelectedViewModel(Group221, Ivanov);

        viewModel.GenerateCommand.Execute(null);
        viewModel.SelectedGroup = Group222;
        viewModel.SelectedGroup = Group221;
        viewModel.SelectedStudent = Ivanov;
        viewModel.GenerateCommand.Execute(null);

        Assert.Equal(["Ваш билет № 2", "Ваш билет № 2"], _shownResults.Select(result => result.Title));
    }

    [Fact]
    public void Criterion6_RecordIsSavedBeforeResultIsShown()
    {
        var viewModel = CreateSelectedViewModel(Group221, Ivanov);
        var recordsWhenShown = -1;
        _presenter.When(presenter => presenter.Show(Arg.Any<ResultViewModel>()))
            .Do(_ => recordsWhenShown = _journal.Records.Count);

        viewModel.GenerateCommand.Execute(null);

        Assert.Equal(1, recordsWhenShown);
    }

    [Fact]
    public void Criterion7_MissingStudentsFile_ShowsMessageWithoutException()
    {
        _studentSource.GetGroups().Returns(_ => throw new SourceFileMissingException("students.xlsx"));
        var viewModel = CreateViewModel();

        var initialized = viewModel.Initialize();

        Assert.False(initialized);
        Assert.Empty(viewModel.Groups);
        _notifier.Received(1).ShowError(Arg.Is<string>(message => message.Contains("students.xlsx")));
    }

    [Fact]
    public void Criterion7_StudentsFileRemovedWhileRunning_ShowsMessageAndEmptyList()
    {
        _studentSource.GetStudents(Group221).Returns(_ => throw new SourceFileMissingException("students.xlsx"));
        var viewModel = CreateInitializedViewModel();

        viewModel.SelectedGroup = Group221;

        Assert.Empty(viewModel.Students);
        _notifier.Received(1).ShowError(Arg.Any<string>());
    }

    [Fact]
    public void Criterion7_LockedJournal_AsksToRetryAndSucceedsAfterUnlock()
    {
        var assigner = Substitute.For<ITicketAssigner>();
        var assignment = new TicketAssignment(
            Tickets[0],
            new ResultRecord(Group221, "Иванов", "Иван", 1, DateTime.Now, IsRepeat: false));
        assigner.Assign(Group221, Ivanov).Returns(
            _ => throw new JournalUnavailableException("results.xlsx", new IOException()),
            _ => assignment);
        _notifier.AskRetry(Arg.Any<string>()).Returns(true);
        var viewModel = CreateSelectedViewModel(Group221, Ivanov, assigner);

        viewModel.GenerateCommand.Execute(null);

        _notifier.Received(1).AskRetry(Arg.Is<string>(message => message.Contains("results.xlsx")));
        Assert.Equal("Ваш билет № 1", Assert.Single(_shownResults).Title);
    }

    [Fact]
    public void Criterion7_LockedJournal_RetryDeclined_ReturnsToSelectionWithoutResult()
    {
        var assigner = Substitute.For<ITicketAssigner>();
        assigner.Assign(Group221, Ivanov).Returns(
            _ => throw new JournalUnavailableException("results.xlsx", new IOException()));
        _notifier.AskRetry(Arg.Any<string>()).Returns(false);
        var viewModel = CreateSelectedViewModel(Group221, Ivanov, assigner);

        viewModel.GenerateCommand.Execute(null);

        Assert.Empty(_shownResults);
        Assert.True(viewModel.GenerateCommand.CanExecute(null));
    }

    [Fact]
    public void TicketMissingFromFile_ShowsMessageWithoutResult()
    {
        _journal.Append(new ResultRecord(Group221, "Иванов", "Иван", 42, DateTime.Now, IsRepeat: false));
        var viewModel = CreateSelectedViewModel(Group221, Ivanov);

        viewModel.GenerateCommand.Execute(null);

        Assert.Empty(_shownResults);
        _notifier.Received(1).ShowError(Arg.Is<string>(message => message.Contains("42")));
    }

    [Fact]
    public void Criterion8_RepeatedStudent_AppendsNewRowWithFirstNumberAndYes()
    {
        _randomSource.Next(1, 4).Returns(3, 1);
        var viewModel = CreateSelectedViewModel(Group221, Ivanov);

        viewModel.GenerateCommand.Execute(null);
        viewModel.GenerateCommand.Execute(null);

        Assert.Equal(2, _journal.Records.Count);
        Assert.Equal((3, false), (_journal.Records[0].TicketNumber, _journal.Records[0].IsRepeat));
        Assert.Equal((3, true), (_journal.Records[1].TicketNumber, _journal.Records[1].IsRepeat));
    }

    [Fact]
    public void Criterion9_JournalKeepsAllGenerationsInChronologicalOrder()
    {
        _randomSource.Next(1, 4).Returns(1, 2);
        var viewModel = CreateSelectedViewModel(Group221, Ivanov);

        viewModel.GenerateCommand.Execute(null);
        viewModel.SelectedGroup = Group222;
        viewModel.SelectedStudent = Petrov;
        viewModel.GenerateCommand.Execute(null);
        viewModel.SelectedGroup = Group221;
        viewModel.SelectedStudent = Ivanov;
        viewModel.GenerateCommand.Execute(null);

        Assert.Equal(
            ["Иванов/нет", "Петров/нет", "Иванов/да"],
            _journal.Records.Select(record => $"{record.LastName}/{(record.IsRepeat ? "да" : "нет")}"));
        Assert.Equal(
            _journal.Records.Select(record => record.GeneratedAt).Order(),
            _journal.Records.Select(record => record.GeneratedAt));
    }

    private MainViewModel CreateViewModel(ITicketAssigner? assigner = null) =>
        new(
            _studentSource,
            assigner ?? new TicketAssignmentService(Tickets, _journal, _randomSource, _clock),
            _presenter,
            _notifier);

    private MainViewModel CreateInitializedViewModel(ITicketAssigner? assigner = null)
    {
        var viewModel = CreateViewModel(assigner);
        viewModel.Initialize();
        return viewModel;
    }

    private MainViewModel CreateSelectedViewModel(string group, Student student, ITicketAssigner? assigner = null)
    {
        var viewModel = CreateInitializedViewModel(assigner);
        viewModel.SelectedGroup = group;
        viewModel.SelectedStudent = student;
        return viewModel;
    }
}
