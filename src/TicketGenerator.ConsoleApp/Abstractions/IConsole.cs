namespace TicketGenerator.ConsoleApp.Abstractions;

public interface IConsole
{
    void Write(string value);

    void WriteLine(string value);

    string? ReadLine();

    ConsoleKeyInfo ReadKey(bool intercept = true);
}
