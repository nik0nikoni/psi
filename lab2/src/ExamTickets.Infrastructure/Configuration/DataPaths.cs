namespace ExamTickets.Infrastructure.Configuration;

public sealed record DataPaths(string Directory)
{
    public const string DataArgument = "--data";

    public string StudentsFile => Path.Combine(Directory, "students.xlsx");

    public string TicketsFile => Path.Combine(Directory, "tickets.docx");

    public string ResultsFile => Path.Combine(Directory, "results.xlsx");

    public string LogFile => Path.Combine(Directory, "exam-tickets.log");

    public static DataPaths FromArguments(IReadOnlyList<string> arguments, string currentDirectory)
    {
        for (var i = 0; i < arguments.Count - 1; i++)
        {
            if (string.Equals(arguments[i], DataArgument, StringComparison.OrdinalIgnoreCase))
            {
                return new DataPaths(Path.GetFullPath(arguments[i + 1], currentDirectory));
            }
        }

        return new DataPaths(currentDirectory);
    }
}
