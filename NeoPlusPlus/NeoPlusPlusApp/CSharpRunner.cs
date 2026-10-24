using System.Diagnostics;
using System.Text;

namespace NeoPlusPlusApp;

internal sealed record RunResult(string StandardOutput, string StandardError, int ExitCode, bool TimedOut);

internal static class CSharpRunner
{
    private const int MaximumOutputCharacters = 256 * 1024;

    public static async Task<RunResult> RunAsync(string source, TimeSpan executionTimeout)
    {
        var runDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Neo++",
            "Runs",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runDirectory);

        try
        {
            var projectPath = Path.Combine(runDirectory, "Solution.csproj");
            await File.WriteAllTextAsync(projectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources>
                  </PropertyGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(Path.Combine(runDirectory, "Program.cs"), source);

            var build = await StartAndCaptureAsync(
                "dotnet",
                $"build \"{projectPath}\" --nologo --verbosity quiet",
                runDirectory,
                TimeSpan.FromSeconds(30));
            if (build.TimedOut)
            {
                return new RunResult("", "Compilation timed out.", -1, true);
            }

            if (build.ExitCode != 0)
            {
                return new RunResult("", build.StandardError + build.StandardOutput, build.ExitCode, false);
            }

            var assemblyPath = Path.Combine(runDirectory, "bin", "Debug", "net8.0", "Solution.dll");
            return await StartAndCaptureAsync(
                "dotnet",
                $"\"{assemblyPath}\"",
                runDirectory,
                executionTimeout);
        }
        finally
        {
            try
            {
                Directory.Delete(runDirectory, recursive: true);
            }
            catch (IOException)
            {
                // The operating system may still be releasing build files after a timeout.
            }
            catch (UnauthorizedAccessException)
            {
                // A failed cleanup must not hide compiler or runtime output.
            }
        }
    }

    private static async Task<RunResult> StartAndCaptureAsync(
        string executable,
        string arguments,
        string workingDirectory,
        TimeSpan timeout)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Could not start {executable}.");
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        var stdoutTask = ReadBoundedAsync(process.StandardOutput, process);
        var stderrTask = ReadBoundedAsync(process.StandardError, process);
        bool timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new RunResult(stdout, stderr, timedOut ? -1 : process.ExitCode, timedOut);
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        Process process)
    {
        var output = new StringBuilder();
        var buffer = new char[4096];
        while (true)
        {
            int read = await reader.ReadAsync(buffer.AsMemory());
            if (read == 0)
            {
                break;
            }

            int remaining = MaximumOutputCharacters - output.Length;
            if (read > remaining)
            {
                output.Append(buffer, 0, Math.Max(0, remaining));
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
                output.AppendLine().Append("[output truncated: 256 KB limit reached]");
                break;
            }
            output.Append(buffer, 0, read);
        }

        return output.ToString();
    }
}
