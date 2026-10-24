namespace NeoPlusPlusSetup;

internal sealed class SetupForm : Form
{
    private readonly TextBox _destination = new();
    private readonly CheckBox _desktopShortcut = new();
    private readonly ProgressBar _progress = new();
    private readonly Button _install = new();
    private readonly Label _message = new();

    public SetupForm()
    {
        Text = "Neo++ Setup";
        ClientSize = new Size(620, 430);
        MinimumSize = new Size(620, 430);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(20, 23, 32);
        ForeColor = Color.White;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        BuildUi();
    }

    private void BuildUi()
    {
        var brand = new Label
        {
            Text = "Neo++",
            Left = 34,
            Top = 28,
            AutoSize = true,
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = Color.FromArgb(116, 166, 255)
        };
        var heading = new Label
        {
            Text = "Competitive coding, ready to launch.",
            Left = 38,
            Top = 84,
            AutoSize = true,
            Font = new Font("Segoe UI", 13),
            ForeColor = Color.White
        };
        var subtitle = new Label
        {
            Text = "Install the Neo++ desktop app and add it to Windows search.",
            Left = 38,
            Top = 118,
            AutoSize = true,
            ForeColor = Color.FromArgb(175, 184, 201)
        };

        var pathLabel = new Label { Text = "Install location", Left = 38, Top = 180, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _destination.Left = 38;
        _destination.Top = 207;
        _destination.Width = 440;
        _destination.Height = 30;
        _destination.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Neo++");
        _destination.BackColor = Color.FromArgb(13, 15, 21);
        _destination.ForeColor = Color.White;
        var browse = new Button { Text = "Browse…", Left = 488, Top = 206, Width = 92, Height = 32 };
        StyleButton(browse, Color.FromArgb(52, 59, 74));
        browse.Click += (_, _) => BrowseDestination();

        _desktopShortcut.Text = "Create a desktop shortcut";
        _desktopShortcut.Left = 38;
        _desktopShortcut.Top = 258;
        _desktopShortcut.AutoSize = true;
        _desktopShortcut.Checked = true;
        _desktopShortcut.ForeColor = Color.FromArgb(218, 223, 235);

        _message.Text = "Neo++ will also appear in Start and Windows search. No administrator rights are required.";
        _message.Left = 38;
        _message.Top = 292;
        _message.Width = 540;
        _message.Height = 42;
        _message.ForeColor = Color.FromArgb(175, 184, 201);

        _progress.Left = 38;
        _progress.Top = 344;
        _progress.Width = 540;
        _progress.Height = 14;
        _progress.Visible = false;

        _install.Text = "Install Neo++";
        _install.Left = 428;
        _install.Top = 374;
        _install.Width = 150;
        _install.Height = 38;
        StyleButton(_install, Color.FromArgb(69, 130, 246));
        _install.Click += async (_, _) => await InstallAsync();

        Controls.AddRange([brand, heading, subtitle, pathLabel, _destination, browse, _desktopShortcut, _message, _progress, _install]);
    }

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
        _progress.Visible = true;
        _progress.Style = ProgressBarStyle.Marquee;
        _message.Text = "Installing files and registering Neo++ with Windows…";

        try
        {
            var sourceDirectory = AppContext.BaseDirectory;
            var destinationDirectory = Path.GetFullPath(_destination.Text);
            bool createDesktopShortcut = _desktopShortcut.Checked;
            var installedApp = await Task.Run(() => Installer.Install(sourceDirectory, destinationDirectory, createDesktopShortcut));
            _progress.Style = ProgressBarStyle.Continuous;
            _progress.Value = 100;
            _message.Text = "Neo++ installed successfully. Use Start or Windows search to open it.";
            var launch = MessageBox.Show(this, "Neo++ is installed. Launch it now?", "Setup complete", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (launch == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installedApp) { UseShellExecute = true });
            }
            Close();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.Security.SecurityException or System.Runtime.InteropServices.COMException)
        {
            _progress.Visible = false;
            _message.Text = exception.Message;
            _message.ForeColor = Color.FromArgb(255, 145, 145);
            _install.Enabled = true;
            MessageBox.Show(this, exception.Message, "Installation failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void StyleButton(Button button, Color background)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = background;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }
}
