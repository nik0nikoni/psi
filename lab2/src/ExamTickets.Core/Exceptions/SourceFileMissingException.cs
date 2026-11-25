namespace ExamTickets.Core.Exceptions;

public sealed class SourceFileMissingException : Exception
{
    public SourceFileMissingException(string filePath)
        : base($"Не найден файл \"{filePath}\". Положите его в папку с данными и перезапустите приложение.")
    {
        FilePath = filePath;
    }

    public string FilePath { get; }
}
