using ExamTickets.Infrastructure.Configuration;

namespace ExamTickets.UnitTests.Configuration;

public sealed class DataPathsTests
{
    private static readonly string CurrentDirectory = Path.Combine(Path.GetTempPath(), "app");

    [Fact]
    public void FromArguments_NoArguments_UsesCurrentDirectory()
    {
        var paths = DataPaths.FromArguments([], CurrentDirectory);

        Assert.Equal(Path.Combine(CurrentDirectory, "students.xlsx"), paths.StudentsFile);
        Assert.Equal(Path.Combine(CurrentDirectory, "tickets.docx"), paths.TicketsFile);
        Assert.Equal(Path.Combine(CurrentDirectory, "results.xlsx"), paths.ResultsFile);
    }

    [Fact]
    public void FromArguments_DataArgument_UsesGivenFolder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "data");

        var paths = DataPaths.FromArguments(["--data", folder], CurrentDirectory);

        Assert.Equal(Path.Combine(folder, "results.xlsx"), paths.ResultsFile);
    }

    [Fact]
    public void FromArguments_RelativeDataFolder_IsResolvedAgainstCurrentDirectory()
    {
        var paths = DataPaths.FromArguments(["--data", "samples"], CurrentDirectory);

        Assert.Equal(Path.Combine(CurrentDirectory, "samples"), paths.Directory);
    }

    [Fact]
    public void FromArguments_DataArgumentWithoutValue_UsesCurrentDirectory()
    {
        var paths = DataPaths.FromArguments(["--data"], CurrentDirectory);

        Assert.Equal(CurrentDirectory, paths.Directory);
    }
}
