using System.Diagnostics;
using System.Text;

namespace TicketGenerator.E2ETests;

public sealed record ApplicationResult(int ExitCode, string Output, string Error);

public static class ApplicationProcess
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static string ApplicationPath => Path.Combine(AppContext.BaseDirectory, "TicketGenerator.ConsoleApp.dll");

    // Input lines are sent followed by the end of stdin, which the application treats as exit.
    public static ApplicationResult Run(string workingDirectory, params string[] inputLines)
    {
        using var process = Start(workingDirectory);
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        foreach (var line in inputLines)
        {
            process.StandardInput.Write(line + "\n");
        }

        process.StandardInput.Close();

        if (!process.WaitForExit(Timeout))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The application did not exit in time.");
        }

        return new ApplicationResult(process.ExitCode, output.Result, error.Result);
    }

    public static Process Start(string workingDirectory)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8,
            StandardOutputEncoding = Utf8,
            StandardErrorEncoding = Utf8,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(ApplicationPath);

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the application.");
    }
}
