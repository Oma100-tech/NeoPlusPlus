using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace NeoPlusPlusSetup;

internal static class Installer
{
    private const string ProductKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\NeoPlusPlus";
    private const int MoveFileDelayUntilReboot = 0x4;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);

    public static string Install(string sourceDirectory, string destinationDirectory, bool createDesktopShortcut)
    {
        var appSource = Path.Combine(sourceDirectory, "NeoPlusPlus.exe");
        if (!File.Exists(appSource))
        {
            throw new FileNotFoundException("The Neo++ application was not found next to the installer.", appSource);
        }

        Directory.CreateDirectory(destinationDirectory);
        var appTarget = Path.Combine(destinationDirectory, "NeoPlusPlus.exe");
        File.Copy(appSource, appTarget, overwrite: true);

        var setupSource = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(setupSource) &&
            File.Exists(setupSource) &&
            !Path.GetFullPath(setupSource).Equals(Path.GetFullPath(Path.Combine(destinationDirectory, "NeoPlusPlusSetup.exe")), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(setupSource, Path.Combine(destinationDirectory, "NeoPlusPlusSetup.exe"), overwrite: true);
        }

        var startMenu = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "Programs",
            "Neo++");
        Directory.CreateDirectory(startMenu);
        CreateShortcut(Path.Combine(startMenu, "Neo++.lnk"), appTarget);

        if (createDesktopShortcut)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            CreateShortcut(Path.Combine(desktop, "Neo++.lnk"), appTarget);
        }

        using (var appPaths = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\NeoPlusPlus.exe"))
        {
            appPaths?.SetValue(null, appTarget);
            appPaths?.SetValue("Path", destinationDirectory);
        }

        using (var uninstall = Registry.CurrentUser.CreateSubKey(ProductKey))
        {
            uninstall?.SetValue("DisplayName", "Neo++");
            uninstall?.SetValue("DisplayVersion", "1.0.0");
            uninstall?.SetValue("Publisher", "Neo++");
            uninstall?.SetValue("InstallLocation", destinationDirectory);
            uninstall?.SetValue("DisplayIcon", appTarget);
            uninstall?.SetValue("UninstallString", $"\"{Path.Combine(destinationDirectory, "NeoPlusPlusSetup.exe")}\" --uninstall");
            uninstall?.SetValue("NoModify", 1, RegistryValueKind.DWord);
            uninstall?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }

        return appTarget;
    }

    public static void Uninstall()
    {
        string? installDirectory;
        using (var uninstall = Registry.CurrentUser.OpenSubKey(ProductKey))
        {
            installDirectory = uninstall?.GetValue("InstallLocation") as string;
        }

        var runningDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrWhiteSpace(installDirectory) ||
            string.IsNullOrWhiteSpace(runningDirectory) ||
            !Path.GetFullPath(installDirectory).Equals(Path.GetFullPath(runningDirectory), StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(installDirectory))
        {
            MessageBox.Show("Neo++ installation information was not found.", "Neo++ Uninstaller", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirmation = MessageBox.Show(
            "Remove Neo++ and its Start menu shortcuts? Your local profile data will be kept.",
            "Uninstall Neo++",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Neo++");
        if (Directory.Exists(startMenu))
        {
            var startMenuShortcut = Path.Combine(startMenu, "Neo++.lnk");
            if (File.Exists(startMenuShortcut))
            {
                File.Delete(startMenuShortcut);
            }

            if (!Directory.EnumerateFileSystemEntries(startMenu).Any())
            {
                Directory.Delete(startMenu);
            }
        }

        var desktopShortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Neo++.lnk");
        if (File.Exists(desktopShortcut))
        {
            File.Delete(desktopShortcut);
        }

        var appPath = Path.Combine(installDirectory, "NeoPlusPlus.exe");
        if (File.Exists(appPath))
        {
            File.Delete(appPath);
        }

        var setupPath = Path.Combine(installDirectory, "NeoPlusPlusSetup.exe");
        bool setupScheduledForRemoval = true;
        if (File.Exists(setupPath))
        {
            setupScheduledForRemoval = MoveFileEx(setupPath, null, MoveFileDelayUntilReboot);
        }

        using (var appPaths = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths", writable: true))
        {
            appPaths?.DeleteSubKey("NeoPlusPlus.exe", throwOnMissingSubKey: false);
        }

        Registry.CurrentUser.DeleteSubKey(ProductKey, throwOnMissingSubKey: false);
        string message = setupScheduledForRemoval
            ? "Neo++ was removed. Your local profile data was kept."
            : "Neo++ was removed, but Windows could not schedule removal of the setup helper.";
        MessageBox.Show(message, "Neo++ Uninstaller", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host is unavailable; cannot create a Start menu shortcut.");
        object shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Could not initialize Windows Script Host.");
        object shortcut = shellType.InvokeMember(
            "CreateShortcut",
            System.Reflection.BindingFlags.InvokeMethod,
            null,
            shell,
            [shortcutPath])
            ?? throw new InvalidOperationException("Could not create a Neo++ shortcut.");

        try
        {
            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, shortcut, [targetPath]);
            shortcutType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, shortcut, [Path.GetDirectoryName(targetPath)!]);
            shortcutType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, shortcut, ["Neo++ competitive coding"]);
            shortcutType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, shortcut, [$"{targetPath},0"]);
            shortcutType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shortcut);
            Marshal.FinalReleaseComObject(shell);
        }
    }
}
