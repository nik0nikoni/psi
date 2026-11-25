using ExamTickets.Core.Abstractions;

namespace ExamTickets.Infrastructure.Logging;

public sealed class FileAppLogger : IAppLogger
{
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly string _filePath;
    private readonly IClock _clock;

    public FileAppLogger(string filePath, IClock clock)
    {
        _filePath = filePath;
        _clock = clock;
    }

    public void Error(string message)
    {
        try
        {
            File.AppendAllText(_filePath, $"{_clock.Now.ToString(DateTimeFormat)} [ERROR] {message}{Environment.NewLine}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A log that cannot be written must not stop ticket generation.
        }
    }
}
