using TicketGenerator.ConsoleApp.Abstractions;
using TicketGenerator.Core.Exceptions;
using TicketGenerator.Core.Models;
using TicketGenerator.Core.Services;
using TicketGenerator.Core.Validation;

namespace TicketGenerator.ConsoleApp;

public sealed class TicketConsoleApplication
{
    public const string ExitHint = "Для выхода нажмите ESC.";
    public const string LastNamePrompt = "Last name: ";
    public const string FirstNamePrompt = "First name: ";
    public const string EmptyValueMessage = "Значение не может быть пустым. Повторите ввод.";
    public const string RetryPrompt = "Закройте файл и нажмите Enter, чтобы повторить сохранение (ESC — выход): ";

    private readonly IConsole _console;
    private readonly ILineReader _lineReader;
    private readonly StudentNameValidator _validator;
    private readonly TicketIssuingService _service;

    public TicketConsoleApplication(
        IConsole console,
        ILineReader lineReader,
        StudentNameValidator validator,
        TicketIssuingService service)
    {
        _console = console;
        _lineReader = lineReader;
        _validator = validator;
        _service = service;
    }

    public void Run()
    {
        _console.WriteLine(ExitHint);

        while (true)
        {
            var lastName = ReadRequiredValue(LastNamePrompt);
            if (lastName is null)
            {
                return;
            }

            var firstName = ReadRequiredValue(FirstNamePrompt);
            if (firstName is null)
            {
                return;
            }

            var record = IssueWithRetry(lastName, firstName);
            if (record is null)
            {
                return;
            }

            _console.WriteLine($"Билет № {record.TicketNumber}");
        }
    }

    private string? ReadRequiredValue(string prompt)
    {
        while (true)
        {
            _console.Write(prompt);
            var value = _lineReader.ReadLine();

            if (value is null)
            {
                return null;
            }

            if (_validator.TryValidate(value, out var normalizedValue))
            {
                return normalizedValue;
            }

            _console.WriteLine(EmptyValueMessage);
        }
    }

    // The student's data is kept while the journal is locked, so the user only has to close the file.
    private TicketRecord? IssueWithRetry(string lastName, string firstName)
    {
        while (true)
        {
            try
            {
                return _service.Issue(lastName, firstName);
            }
            catch (JournalUnavailableException exception)
            {
                _console.WriteLine(exception.Message);
                _console.Write(RetryPrompt);

                if (_lineReader.ReadLine() is null)
                {
                    return null;
                }
            }
        }
    }
}
