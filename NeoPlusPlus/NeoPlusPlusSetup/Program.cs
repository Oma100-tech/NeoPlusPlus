namespace NeoPlusPlusSetup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("Neo++ setup is only supported on Windows.");
            Environment.ExitCode = 1;
            return;
        }

        ApplicationConfiguration.Initialize();
        if (args.Length > 0 && args[0].Equals("--uninstall", StringComparison.OrdinalIgnoreCase))
        {
            Installer.Uninstall();
            return;
        }

        Application.Run(new SetupForm());
    }
}
