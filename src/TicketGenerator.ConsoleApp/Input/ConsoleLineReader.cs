using System.Text;
using TicketGenerator.ConsoleApp.Abstractions;

namespace TicketGenerator.ConsoleApp.Input;

public sealed class ConsoleLineReader : ILineReader
{
    private readonly IConsole _console;

    public ConsoleLineReader(IConsole console)
    {
        _console = console;
    }

    public string? ReadLine()
    {
        var buffer = new StringBuilder();

        while (true)
        {
            var key = _console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _console.WriteLine(string.Empty);
                    return null;
                case ConsoleKey.Enter:
                    _console.WriteLine(string.Empty);
                    return buffer.ToString();
                case ConsoleKey.Backspace:
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        _console.Write("\b \b");
                    }

                    break;
                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        buffer.Append(key.KeyChar);
                        _console.Write(key.KeyChar.ToString());
                    }

                    break;
            }
        }
    }
}
