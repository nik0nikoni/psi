namespace TicketGenerator.ConsoleApp.Abstractions;

public interface ILineReader
{
    /// <summary>Reads a line confirmed with Enter; returns <c>null</c> when the user presses ESC.</summary>
    string? ReadLine();
}
