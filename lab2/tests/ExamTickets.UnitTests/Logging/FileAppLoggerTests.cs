using ExamTickets.Core.Abstractions;
using ExamTickets.Infrastructure.Logging;
using NSubstitute;

namespace ExamTickets.UnitTests.Logging;

public sealed class FileAppLoggerTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;
    private readonly IClock _clock = Substitute.For<IClock>();

    public FileAppLoggerTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "exam-tickets-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "exam-tickets.log");
        _clock.Now.Returns(new DateTime(2026, 11, 25, 9, 5, 0));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Error_AppendsTimestampedLines()
    {
        var logger = new FileAppLogger(_filePath, _clock);

        logger.Error("Билет 2 пропущен");
        logger.Error("Билет 5 пропущен");

        Assert.Equal(
            ["2026-11-25 09:05:00 [ERROR] Билет 2 пропущен", "2026-11-25 09:05:00 [ERROR] Билет 5 пропущен"],
            File.ReadAllLines(_filePath));
    }

    [Fact]
    public void Error_LockedFile_DoesNotThrow()
    {
        File.WriteAllText(_filePath, "");
        var logger = new FileAppLogger(_filePath, _clock);

        using (new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            logger.Error("Билет 2 пропущен");
        }

        Assert.Empty(File.ReadAllText(_filePath));
    }
}
