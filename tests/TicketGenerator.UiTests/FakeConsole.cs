using System.Text;
using TicketGenerator.ConsoleApp.Abstractions;

namespace TicketGenerator.UiTests;

public sealed class FakeConsole : IConsole
{
    private readonly Queue<Func<ConsoleKeyInfo?>> _input = new();
    private readonly StringBuilder _output = new();

    public string Output => _output.ToString();

    public FakeConsole Type(string value)
    {
        foreach (var character in value)
        {
            _input.Enqueue(() => new ConsoleKeyInfo(character, default, false, false, false));
        }

        return this;
    }

    public FakeConsole TypeLine(string value) => Type(value).PressKey(ConsoleKey.Enter, '\r');

    public FakeConsole PressBackspace() => PressKey(ConsoleKey.Backspace, '\b');

    public FakeConsole PressEscape() => PressKey(ConsoleKey.Escape, '\u001b');

    // Runs an action at this point of the input sequence, e.g. to lock or unlock the journal.
    public FakeConsole Then(Action action)
    {
        _input.Enqueue(() =>
        {
            action();
            return null;
        });

        return this;
    }

    public void Write(string value) => _output.Append(value);

    public void WriteLine(string value) => _output.Append(value).Append('\n');

    public string? ReadLine() => throw new NotSupportedException("The application reads input key by key.");

    public ConsoleKeyInfo ReadKey(bool intercept = true)
    {
        while (_input.TryDequeue(out var next))
        {
            if (next() is { } key)
            {
                return key;
            }
        }

        throw new InvalidOperationException("The application requested more input than the test provided.");
    }

    private FakeConsole PressKey(ConsoleKey key, char character)
    {
        _input.Enqueue(() => new ConsoleKeyInfo(character, key, false, false, false));
        return this;
    }
}
