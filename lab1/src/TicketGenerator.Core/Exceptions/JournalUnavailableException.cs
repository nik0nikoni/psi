namespace TicketGenerator.Core.Exceptions;

public sealed class JournalUnavailableException : Exception
{
    public JournalUnavailableException(string filePath, Exception innerException)
        : base(
            $"Файл журнала \"{filePath}\" недоступен для записи. " +
            "Возможно, он открыт в Excel. Закройте файл и повторите ввод.",
            innerException)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
