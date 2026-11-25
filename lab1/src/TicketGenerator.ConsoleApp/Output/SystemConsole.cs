using TicketGenerator.ConsoleApp.Abstractions;

namespace TicketGenerator.ConsoleApp.Output;

public sealed class SystemConsole : IConsole
{
    public void Write(string value) => Console.Write(value);

    public void WriteLine(string value) => Console.WriteLine(value);

    public string? ReadLine() => Console.ReadLine();

    public ConsoleKeyInfo ReadKey(bool intercept = true)
    {
        return Console.IsInputRedirected ? ReadRedirectedKey() : Console.ReadKey(intercept);
    }

    // Console.ReadKey is unavailable for redirected input, so keys are built from characters.
    // The end of the input stream is reported as ESC so that the application exits normally.
    private static ConsoleKeyInfo ReadRedirectedKey()
    {
        var value = Console.In.Read();

        switch (value)
        {
            case -1:
            case 27:
                return new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false);
            case '\r':
                if (Console.In.Peek() == '\n')
                {
                    Console.In.Read();
                }

                return new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
            case '\n':
                return new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false);
            case '\b':
                return new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false);
            default:
                return new ConsoleKeyInfo((char)value, default, false, false, false);
        }
    }
}
