namespace NeoPlusPlusSetup;

internal sealed class SetupForm : Form
{
    private readonly TextBox _destination = new();
    private readonly CheckBox _installCli = new();
    private readonly CheckBox _installExamples = new();
    private readonly CheckBox _desktopShortcut = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _install = new();
    private readonly Label _message = new();
    private readonly Label _progressDetails = new();
    private readonly Label _sizeEstimate = new();

    public SetupForm()
    {
        Text = "Neo++ Installer";
        ClientSize = new Size(820, 590);
        MinimumSize = new Size(820, 590);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 250);
        ForeColor = Color.FromArgb(36, 40, 48);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        BuildUi();
        UpdateSizeEstimate();
    }

    private void BuildUi()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 236,
            BackColor = Color.FromArgb(24, 48, 82)
        };
        var brand = new Label
        {
            Text = "Neo++",
            Left = 28,
            Top = 36,
            AutoSize = true,
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = Color.White
        };
        var sidebarHeading = new Label
        {
            Text = "DEVELOPMENT\nENVIRONMENT",
            Left = 32,
            Top = 102,
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 211, 245)
        };
        var sidebarDescription = new Label
        {
            Text = "Create, run, and explore programs built with the Neo++ language and NVM.",
            Left = 32,
            Top = 170,
            Width = 172,
            Height = 104,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(220, 229, 242)
        };
        var version = new Label
        {
            Text = "Neo++ Setup  |  Windows x64",
            Left = 24,
            Top = 540,
            AutoSize = true,
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(190, 211, 245)
        };
        sidebar.Controls.AddRange([brand, sidebarHeading, sidebarDescription, version]);
        Controls.Add(sidebar);

        var heading = new Label
        {
            Text = "Choose what to install",
            Left = 270,
            Top = 30,
            AutoSize = true,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 42, 57)
        };
        var subtitle = new Label
        {
            Text = "Select the Neo++ features you want. The IDE is required.",
            Left = 272,
            Top = 68,
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(91, 101, 117)
        };

        var components = new GroupBox
        {
            Text = "Features",
            Left = 270,
            Top = 110,
            Width = 520,
            Height = 200,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 52, 62)
        };
        var ide = new CheckBox
        {
            Text = "Neo++ IDE  (required)",
            Left = 18,
            Top = 30,
            AutoSize = true,
            Checked = true,
            Enabled = false,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(36, 40, 48)
        };
        var ideDescription = ComponentDescription("Editor, visual workspace, and NVM runtime.", 56);

        _installCli.Text = "Command-line and development tools";
        _installCli.Left = 18;
        _installCli.Top = 87;
        _installCli.AutoSize = true;
        _installCli.Checked = true;
        _installCli.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _installCli.CheckedChanged += (_, _) => UpdateSizeEstimate();
        var cliDescription = ComponentDescription("Run .neo files and inspect bytecode from a terminal.", 112);

        _installExamples.Text = "Example programs";
        _installExamples.Left = 18;
        _installExamples.Top = 143;
        _installExamples.AutoSize = true;
        _installExamples.Checked = true;
        _installExamples.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _installExamples.CheckedChanged += (_, _) => UpdateSizeEstimate();
        components.Controls.AddRange([ide, ideDescription, _installCli, cliDescription, _installExamples]);

        var locationLabel = new Label
        {
            Text = "Install location",
            Left = 270,
            Top = 332,
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 52, 62)
        };
        _destination.SetBounds(270, 360, 412, 32);
        _destination.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Neo++");
        _destination.Font = new Font("Segoe UI", 10);
        var browse = new Button { Text = "Browse...", Left = 694, Top = 359, Width = 96, Height = 34 };
        StyleButton(browse, Color.FromArgb(229, 233, 239), Color.FromArgb(43, 52, 67));
        browse.Click += (_, _) => BrowseDestination();

        _desktopShortcut.Text = "Create a desktop shortcut";
        _desktopShortcut.SetBounds(270, 408, 240, 24);
        _desktopShortcut.AutoSize = true;
        _desktopShortcut.Checked = true;
        _desktopShortcut.Font = new Font("Segoe UI", 9.5F);
        _desktopShortcut.CheckedChanged += (_, _) => UpdateSizeEstimate();

        _sizeEstimate.SetBounds(270, 439, 520, 24);
        _sizeEstimate.ForeColor = Color.FromArgb(91, 101, 117);
        _sizeEstimate.Font = new Font("Segoe UI", 9);

        _progress.SetBounds(270, 478, 520, 16);
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Visible = false;
        _progressDetails.SetBounds(270, 498, 520, 20);
        _progressDetails.ForeColor = Color.FromArgb(91, 101, 117);
        _progressDetails.Font = new Font("Segoe UI", 8.5F);
        _progressDetails.Visible = false;

        _message.SetBounds(270, 522, 340, 42);
        _message.Text = "Neo++ will be available from Start and Windows Search.";
        _message.ForeColor = Color.FromArgb(91, 101, 117);
        _message.Font = new Font("Segoe UI", 8.5F);
        _install.Text = "Install Neo++";
        _install.SetBounds(646, 520, 144, 40);
        StyleButton(_install, Color.FromArgb(0, 102, 204), Color.White);
        _install.Click += async (_, _) => await InstallAsync();

        Controls.AddRange([heading, subtitle, components, locationLabel, _destination, browse, _desktopShortcut, _sizeEstimate, _progress, _progressDetails, _message, _install]);
    }

    private static Label ComponentDescription(string text, int top) => new()
    {
        Text = text,
        Left = 42,
        Top = top,
        AutoSize = true,
        Font = new Font("Segoe UI", 8.5F),
        ForeColor = Color.FromArgb(91, 101, 117)
    };

    private void UpdateSizeEstimate()
    {
        long bytes = GetSourceSize("NeoPlusPlus.exe");
        bytes += GetSourceSize("NeoPlusPlusSetup.exe");
        if (_installCli.Checked)
        {
            bytes += GetSourceSize("NeoPlusPlusCli.exe");
        }
        if (_installExamples.Checked)
        {
            string examples = Path.Combine(AppContext.BaseDirectory, "examples");
            if (Directory.Exists(examples))
            {
                bytes += Directory.EnumerateFiles(examples, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
            }
        }

        _sizeEstimate.Text = $"Approximate space required: {FormatSize(bytes)}";
    }

    private static long GetSourceSize(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    private static string FormatSize(long bytes) => bytes >= 1024L * 1024
        ? $"{bytes / (1024d * 1024):F0} MB"
        : $"{bytes / 1024d:F0} KB";

    private void BrowseDestination()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose where Neo++ should be installed",
            SelectedPath = _destination.Text,
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _destination.Text = dialog.SelectedPath;
        }
    }

    private async Task InstallAsync()
    {
        if (string.IsNullOrWhiteSpace(_destination.Text))
        {
            MessageBox.Show(this, "Choose an installation folder.", "Neo++ Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _install.Enabled = false;
        _install.Text = "Installing...";
        _progress.Visible = true;
        _progress.Value = 0;
        _progressDetails.Visible = true;
        _message.Text = "Preparing selected components...";
        _message.ForeColor = Color.FromArgb(91, 101, 117);

        try
        {
            var sourceDirectory = AppContext.BaseDirectory;
            var destinationDirectory = Path.GetFullPath(_destination.Text);
            bool createDesktopShortcut = _desktopShortcut.Checked;
            bool installCli = _installCli.Checked;
            bool installExamples = _installExamples.Checked;
            var reporter = new Progress<InstallerProgress>(update =>
            {
                _progress.Value = update.Percentage;
                _progressDetails.Text = $"{update.Percentage}%  |  {FormatSize(update.CompletedBytes)} of {FormatSize(update.TotalBytes)}  |  {update.Message}";
            });
            var installedApp = await Task.Run(() =>
                Installer.Install(sourceDirectory, destinationDirectory, createDesktopShortcut, installCli, installExamples, reporter));
            _progress.Value = 100;
            _message.Text = "Neo++ installed successfully.";
            _progressDetails.Text = "Setup complete.";
            var launch = MessageBox.Show(this, "Neo++ is installed. Launch it now?", "Setup complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (launch == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installedApp) { UseShellExecute = true });
            }
            Close();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Security.SecurityException or System.Runtime.InteropServices.COMException)
        {
            _message.Text = exception.Message;
            _message.ForeColor = Color.FromArgb(190, 45, 45);
            _progressDetails.Text = "Installation stopped.";
            _install.Enabled = true;
            _install.Text = "Try again";
            MessageBox.Show(this, exception.Message, "Installation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void StyleButton(Button button, Color background, Color foreground)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = background;
        button.ForeColor = foreground;
        button.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }
}
