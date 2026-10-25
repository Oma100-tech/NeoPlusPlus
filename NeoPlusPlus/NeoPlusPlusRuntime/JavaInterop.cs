using System.Diagnostics;
using System.Text;

namespace NeoPlusPlus.Runtime;

public sealed record JavaRunResult(string Output, string Error, int ExitCode);

public static class NeoJavaBridge
{
    private const int OutputLimit = 128 * 1024;

    public static async Task<JavaRunResult> RunSourceAsync(string sourcePath, IEnumerable<string> arguments, string workingDirectory, TimeSpan timeout)
    {
        string fullSourcePath = Path.GetFullPath(sourcePath, workingDirectory);
        if (!File.Exists(fullSourcePath))
            return new JavaRunResult(string.Empty, $"Java source file not found: {fullSourcePath}", 2);

        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveJavaExecutable(),
            WorkingDirectory = Path.GetDirectoryName(fullSourcePath)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(fullSourcePath);
        foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start()) throw new InvalidOperationException("Could not start the Java runtime.");
        var outputTask = ReadBoundedAsync(process.StandardOutput, process);
        var errorTask = ReadBoundedAsync(process.StandardError, process);
        using var timeoutSource = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            var output = await outputTask;
            var error = await errorTask;
            return new JavaRunResult(output, error + Environment.NewLine + "Java execution timed out.", 124);
        }

        return new JavaRunResult(await outputTask, await errorTask, process.ExitCode);
    }

    public static (string Executable, string Version) FindRuntime()
    {
        var executable = ResolveJavaExecutable();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(executable, "-version")
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        string output = process.StandardError.ReadToEnd();
        string standardOutput = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);
        string version = (output + standardOutput).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Java version unavailable";
        return (executable, version);
    }

    private static string ResolveJavaExecutable()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var java = Path.Combine(javaHome, "bin", OperatingSystem.IsWindows() ? "java.exe" : "java");
            if (File.Exists(java)) return java;
        }
        return OperatingSystem.IsWindows() ? "java.exe" : "java";
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, Process process)
    {
        var output = new StringBuilder();
        var buffer = new char[4096];
        while (true)
        {
            int count = await reader.ReadAsync(buffer.AsMemory());
            if (count == 0) break;
            int available = OutputLimit - output.Length;
            output.Append(buffer, 0, Math.Min(count, Math.Max(available, 0)));
            if (count > available)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                output.AppendLine().Append("[output truncated: 128 KB limit reached]");
                break;
            }
        }
        return output.ToString();
    }

    public static JavaRunResult RunSource(string sourcePath, IEnumerable<string> arguments, string workingDirectory) =>
        RunSourceAsync(sourcePath, arguments, workingDirectory, TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
}
