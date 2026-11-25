using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;

namespace ExamTickets.E2ETests;

// Starts the real WPF application on a data folder and drives it through UI Automation.
public sealed class ApplicationSession : IDisposable
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    private readonly Application _application;
    private readonly UIA3Automation _automation = new();

    private ApplicationSession(string dataDirectory)
    {
        var startInfo = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "ExamTickets.Wpf.exe"))
        {
            WorkingDirectory = dataDirectory,
        };
        startInfo.ArgumentList.Add("--data");
        startInfo.ArgumentList.Add(dataDirectory);

        _application = Application.Launch(startInfo);
    }

    public bool HasExited => _application.HasExited;

    public int ExitCode => _application.ExitCode;

    public static ApplicationSession Start(string dataDirectory) => new(dataDirectory);

    public MainWindowDriver GetMainWindow()
    {
        var window = _application.GetMainWindow(_automation, Timeout)
            ?? throw new InvalidOperationException("The main window did not appear.");

        return new MainWindowDriver(window);
    }

    // Returns the first top-level window of the application, e.g. an error message shown before the main window.
    public Window WaitForTopLevelWindow() =>
        Retry.WhileNull(
                () => _application.GetAllTopLevelWindows(_automation).FirstOrDefault(),
                Timeout,
                throwOnTimeout: true)
            .Result!;

    public bool WaitForExit() => Retry.WhileFalse(() => _application.HasExited, Timeout).Result;

    public void Dispose()
    {
        if (!_application.HasExited)
        {
            _application.Close();
            if (!Retry.WhileFalse(() => _application.HasExited, TimeSpan.FromSeconds(5)).Result)
            {
                _application.Kill();
            }
        }

        _application.Dispose();
        _automation.Dispose();
    }
}

public sealed class MainWindowDriver
{
    private readonly Window _window;

    public MainWindowDriver(Window window)
    {
        _window = window;
    }

    public Window Window => _window;

    public ComboBox Groups => FindById("GroupComboBox").AsComboBox();

    public ComboBox Students => FindById("StudentComboBox").AsComboBox();

    public Button GenerateButton => FindById("GenerateButton").AsButton();

    public IReadOnlyList<string> GroupNames => ItemNames(Groups);

    public IReadOnlyList<string> StudentNames => ItemNames(Students);

    public void SelectGroup(string group) => Groups.Select(group);

    public void SelectStudent(string fullName) => Students.Select(fullName);

    public ResultWindowDriver Generate()
    {
        GenerateButton.Invoke();
        return new ResultWindowDriver(WaitForModalWindow());
    }

    public Window WaitForModalWindow() =>
        Retry.WhileNull(() => _window.ModalWindows.FirstOrDefault(), ApplicationSession.Timeout, throwOnTimeout: true)
            .Result!;

    public bool WaitUntilNoModalWindow() =>
        Retry.WhileFalse(() => _window.ModalWindows.Length == 0, ApplicationSession.Timeout).Result;

    private AutomationElement FindById(string automationId) =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId(automationId))
        ?? throw new InvalidOperationException($"Element '{automationId}' was not found.");

    private static IReadOnlyList<string> ItemNames(ComboBox comboBox)
    {
        comboBox.Expand();
        var names = comboBox.Items.Select(item => item.Name).ToList();
        comboBox.Collapse();
        return names;
    }
}

public sealed class ResultWindowDriver
{
    private readonly Window _window;

    public ResultWindowDriver(Window window)
    {
        _window = window;
    }

    public string Title => FindById("TicketTitle").Name;

    public IReadOnlyList<string> Questions =>
        FindById("QuestionsList")
            .FindAllDescendants(condition => condition.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
            .Select(element => element.Name)
            .ToList();

    public void PressEscape()
    {
        _window.Focus();
        Keyboard.Type(VirtualKeyShort.ESCAPE);
    }

    private AutomationElement FindById(string automationId) =>
        _window.FindFirstDescendant(condition => condition.ByAutomationId(automationId))
        ?? throw new InvalidOperationException($"Element '{automationId}' was not found.");
}
