using NeoPlusPlus.Runtime;

namespace NeoPlusPlusApp;

public partial class Form1 : Form
{
    private readonly List<Challenge> _challenges = [];
    private readonly ProfileStore _profileStore = new();
    private readonly ListBox _challengeList = new();
    private readonly ComboBox _categoryFilter = new();
    private readonly ComboBox _difficultyFilter = new();
    private readonly RichTextBox _editor = new();
    private readonly RichTextBox _output = new();
    private readonly RichTextBox _developerConsoleOutput = new();
    private readonly TextBox _developerConsoleInput = new();
    private readonly List<string> _developerConsoleHistory = [];
    private readonly TabControl _mainTabs = new();
    private readonly Label _status = new();
    private readonly Label _profileSummary = new();
    private readonly DataGridView _leaderboard = new();
    private readonly Button _runButton = new();
    private readonly Label _runtimeSummary = new();
    private readonly TextBox _javaSourcePath = new();
    private readonly RichTextBox _javaOutput = new();
    private string? _currentFilePath;
    private string? _loadedChallengeId;
    private int _developerConsoleHistoryIndex;
    private UserProfile _profile = new();

    public Form1()
    {
        InitializeComponent();
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        _profile = _profileStore.Load();
        LoadChallenges();
        BuildUi();
        RefreshChallengeList();
        RefreshProfileViews();
    }

    private void BuildUi()
    {
        Text = "Neo++ | Competitive coding";

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(10),
            BackColor = Color.FromArgb(17, 19, 26)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        var menu = BuildMainMenu();
        var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(17, 19, 26) };
        var brand = new Label
        {
            Text = "Neo++",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(116, 166, 255)
        };
        var tagline = new Label
        {
            Text = "COMPETITIVE CODING STUDIO",
            Dock = DockStyle.Left,
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(153, 161, 178),
            Padding = new Padding(14, 20, 0, 0)
        };
        header.Controls.Add(tagline);
        header.Controls.Add(brand);

        _mainTabs.Dock = DockStyle.Fill;
        _mainTabs.Font = new Font("Segoe UI", 10F);
        var arena = new TabPage("Arena") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var hub = new TabPage("Runtime Hub") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var java = new TabPage("Java Bridge") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var profile = new TabPage("Profile") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var leaderboard = new TabPage("Leaderboard") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var developerConsole = new TabPage("Developer Console") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        BuildArena(arena);
        BuildRuntimeHub(hub);
        BuildJavaBridge(java);
        BuildProfile(profile);
        BuildLeaderboard(leaderboard);
        BuildDeveloperConsole(developerConsole);
        _mainTabs.TabPages.AddRange([arena, hub, java, developerConsole, profile, leaderboard]);

        _status.Text = "Ready";
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(145, 220, 166);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(menu, 0, 0);
        root.Controls.Add(header, 0, 1);
        root.Controls.Add(_mainTabs, 0, 2);
        root.Controls.Add(_status, 0, 3);
        Controls.Add(root);
    }

    private MenuStrip BuildMainMenu()
    {
        var menu = new MenuStrip
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(30, 33, 43),
            ForeColor = Color.FromArgb(225, 232, 246),
            Font = new Font("Segoe UI", 9F),
            Padding = new Padding(6, 2, 0, 2)
        };
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(MenuItem("&New", Keys.Control | Keys.N, (_, _) => NewFile()));
        file.DropDownItems.Add(MenuItem("&Open...", Keys.Control | Keys.O, (_, _) => OpenCodeFile()));
        file.DropDownItems.Add(MenuItem("&Save...", Keys.Control | Keys.S, (_, _) => SaveCodeFile()));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(MenuItem("E&xit", Keys.None, (_, _) => Close()));

        var run = new ToolStripMenuItem("&Run");
        run.DropDownItems.Add(MenuItem("&Run Neo++", Keys.F5, async (_, _) => await RunNeoProgramAsync(submit: false)));
        run.DropDownItems.Add(MenuItem("Inspect &NVM bytecode", Keys.None, (_, _) => InspectBytecode()));
        run.DropDownItems.Add(MenuItem("Clear developer console", Keys.None, (_, _) => _developerConsoleOutput.Clear()));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(MenuItem("&Developer Console", Keys.Control | Keys.Alt | Keys.C, (_, _) => SelectTab("Developer Console")));

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(MenuItem("Neo++ &Runtime", Keys.None, (_, _) => SelectTab("Runtime Hub")));

        menu.Items.AddRange([file, run, view, help]);
        MainMenuStrip = menu;
        return menu;
    }

    private static ToolStripMenuItem MenuItem(string text, Keys shortcut, EventHandler handler)
    {
        var item = new ToolStripMenuItem(text) { ShortcutKeys = shortcut };
        item.Click += handler;
        return item;
    }

    private void SelectTab(string title)
    {
        var page = _mainTabs.TabPages.Cast<TabPage>().FirstOrDefault(tab => tab.Text == title);
        if (page is not null)
        {
            _mainTabs.SelectedTab = page;
        }
    }

    private void BuildArena(TabPage page)
    {
        var workspace = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 270,
            BackColor = Color.FromArgb(24, 27, 36),
            FixedPanel = FixedPanel.Panel1
        };
        page.Controls.Add(workspace);

        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 4,
            ColumnCount = 1,
            BackColor = Color.FromArgb(29, 33, 44)
        };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.Controls.Add(Heading("Challenges"), 0, 0);

        StyleFilter(_categoryFilter);
        StyleFilter(_difficultyFilter);
        _categoryFilter.SelectedIndexChanged += (_, _) => RefreshChallengeList();
        _difficultyFilter.SelectedIndexChanged += (_, _) => RefreshChallengeList();
        sidebar.Controls.Add(_categoryFilter, 0, 1);
        sidebar.Controls.Add(_difficultyFilter, 0, 2);

        _challengeList.Dock = DockStyle.Fill;
        _challengeList.BackColor = Color.FromArgb(18, 21, 29);
        _challengeList.ForeColor = Color.FromArgb(232, 236, 246);
        _challengeList.BorderStyle = BorderStyle.None;
        _challengeList.Font = new Font("Segoe UI", 10F);
        _challengeList.SelectedIndexChanged += (_, _) => LoadSelectedChallenge();
        sidebar.Controls.Add(_challengeList, 0, 3);
        workspace.Panel1.Controls.Add(sidebar);

        var right = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 390,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        workspace.Panel2.Controls.Add(right);

        var editorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        editorPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        editorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };
        toolbar.Controls.Add(Heading("Neo++ Source"));
        toolbar.Controls.Add(StyledButton("New", (_, _) => NewFile()));
        toolbar.Controls.Add(StyledButton("Open", (_, _) => OpenCodeFile()));
        toolbar.Controls.Add(StyledButton("Save", (_, _) => SaveCodeFile()));
        toolbar.Controls.Add(StyledButton("NVM bytecode", (_, _) => InspectBytecode()));
        _runButton.Text = "Run Neo++";
        StyleButton(_runButton, Color.FromArgb(69, 130, 246));
        _runButton.Click += async (_, _) => await RunNeoProgramAsync(submit: false);
        toolbar.Controls.Add(_runButton);
        toolbar.Controls.Add(StyledButton("Submit solution", async (_, _) => await RunNeoProgramAsync(submit: true)));
        editorPanel.Controls.Add(toolbar, 0, 0);

        _editor.Dock = DockStyle.Fill;
        _editor.BackColor = Color.FromArgb(15, 17, 23);
        _editor.ForeColor = Color.FromArgb(225, 232, 246);
        _editor.Font = new Font("Cascadia Code", 11F);
        _editor.BorderStyle = BorderStyle.FixedSingle;
        _editor.AcceptsTab = true;
        _editor.WordWrap = false;
        _editor.ScrollBars = RichTextBoxScrollBars.Both;
        editorPanel.Controls.Add(_editor, 0, 1);
        right.Panel1.Controls.Add(editorPanel);

        var outputPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        outputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        outputPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outputPanel.Controls.Add(Heading("Run output"), 0, 0);
        _output.Dock = DockStyle.Fill;
        _output.ReadOnly = true;
        _output.BackColor = Color.FromArgb(12, 14, 19);
        _output.ForeColor = Color.FromArgb(180, 210, 190);
        _output.Font = new Font("Cascadia Code", 10F);
        _output.BorderStyle = BorderStyle.FixedSingle;
        _output.ScrollBars = RichTextBoxScrollBars.Both;
        outputPanel.Controls.Add(_output, 0, 1);
        right.Panel2.Controls.Add(outputPanel);

        _categoryFilter.Items.Add("All categories");
        _categoryFilter.Items.AddRange(_challenges.Select(c => c.Category).Distinct().Order().Cast<object>().ToArray());
        _categoryFilter.SelectedIndex = 0;
        _difficultyFilter.Items.AddRange(["All difficulties", "Easy", "Medium", "Hard"]);
        _difficultyFilter.SelectedIndex = 0;
    }

    private void BuildRuntimeHub(TabPage page)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(28),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        layout.Controls.Add(Heading("Neo++ Hub Runtime"), 0, 0);

        _runtimeSummary.Dock = DockStyle.Fill;
        _runtimeSummary.AutoSize = false;
        _runtimeSummary.Font = new Font("Cascadia Code", 11F);
        _runtimeSummary.ForeColor = Color.FromArgb(221, 228, 242);
        _runtimeSummary.Padding = new Padding(18);
        _runtimeSummary.BackColor = Color.FromArgb(15, 17, 23);
        _runtimeSummary.Text = "Loading runtime information…";
        layout.Controls.Add(_runtimeSummary, 0, 1);
        layout.Controls.Add(StyledButton("Refresh runtime status", (_, _) => RefreshRuntimeSummary()), 0, 2);
        page.Controls.Add(layout);
        RefreshRuntimeSummary();
    }

    private void BuildJavaBridge(TabPage page)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(26),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(Heading("NeoJava Interoperability"), 0, 0);

        var filePicker = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        _javaSourcePath.Width = 540;
        _javaSourcePath.ReadOnly = true;
        _javaSourcePath.BackColor = Color.FromArgb(15, 17, 23);
        _javaSourcePath.ForeColor = Color.White;
        filePicker.Controls.Add(_javaSourcePath);
        filePicker.Controls.Add(StyledButton("Choose .java file", (_, _) => ChooseJavaFile()));
        layout.Controls.Add(filePicker, 0, 1);
        layout.Controls.Add(StyledButton("Compile & run with local JDK", async (_, _) => await RunJavaFileAsync()), 0, 2);

        _javaOutput.Dock = DockStyle.Fill;
        _javaOutput.ReadOnly = true;
        _javaOutput.BackColor = Color.FromArgb(12, 14, 19);
        _javaOutput.ForeColor = Color.FromArgb(190, 213, 195);
        _javaOutput.Font = new Font("Cascadia Code", 10F);
        _javaOutput.BorderStyle = BorderStyle.FixedSingle;
        _javaOutput.Text = "Choose a Java source file to run it with the installed JDK. Neo++ scripts can also call java(\"Example.java\").";
        layout.Controls.Add(_javaOutput, 0, 3);
        page.Controls.Add(layout);
    }

    private void BuildDeveloperConsole(TabPage page)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight
        };
        toolbar.Controls.Add(Heading("Neo++ Developer Console"));
        toolbar.Controls.Add(StyledButton("Help", (_, _) => AppendConsoleLine("Commands: help, clear, version, run [file.neo], build [file.neo], disassemble [file.neo]")));
        toolbar.Controls.Add(StyledButton("Clear", (_, _) => _developerConsoleOutput.Clear()));
        toolbar.Controls.Add(StyledButton("Run editor", async (_, _) => await RunConsoleSourceAsync(_editor.Text, _currentFilePath)));
        layout.Controls.Add(toolbar, 0, 0);

        _developerConsoleOutput.Dock = DockStyle.Fill;
        _developerConsoleOutput.ReadOnly = true;
        _developerConsoleOutput.BackColor = Color.FromArgb(12, 14, 19);
        _developerConsoleOutput.ForeColor = Color.FromArgb(205, 215, 232);
        _developerConsoleOutput.Font = new Font("Cascadia Code", 10F);
        _developerConsoleOutput.BorderStyle = BorderStyle.FixedSingle;
        _developerConsoleOutput.WordWrap = false;
        _developerConsoleOutput.Text = "Neo++ Developer Console — commands run locally in the NVM." +
            Environment.NewLine + "Type help to see available commands.";
        layout.Controls.Add(_developerConsoleOutput, 0, 1);

        var commandRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 8, 0, 0) };
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        commandRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var prompt = new Label
        {
            Text = "neo++ >",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(116, 166, 255),
            Font = new Font("Cascadia Code", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _developerConsoleInput.Dock = DockStyle.Fill;
        _developerConsoleInput.BackColor = Color.FromArgb(15, 17, 23);
        _developerConsoleInput.ForeColor = Color.White;
        _developerConsoleInput.Font = new Font("Cascadia Code", 10F);
        _developerConsoleInput.BorderStyle = BorderStyle.FixedSingle;
        _developerConsoleInput.KeyDown += DeveloperConsoleInput_KeyDown;
        commandRow.Controls.Add(prompt, 0, 0);
        commandRow.Controls.Add(_developerConsoleInput, 1, 0);
        layout.Controls.Add(commandRow, 0, 2);
        page.Controls.Add(layout);
    }

    private void DeveloperConsoleInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            string command = _developerConsoleInput.Text.Trim();
            _developerConsoleInput.Clear();
            if (command.Length > 0)
            {
                _developerConsoleHistory.Add(command);
                _developerConsoleHistoryIndex = _developerConsoleHistory.Count;
                _ = ExecuteDeveloperCommandAsync(command);
            }
        }
        else if (e.KeyCode == Keys.Up && _developerConsoleHistory.Count > 0)
        {
            _developerConsoleHistoryIndex = Math.Max(0, _developerConsoleHistoryIndex - 1);
            _developerConsoleInput.Text = _developerConsoleHistory[_developerConsoleHistoryIndex];
            _developerConsoleInput.SelectionStart = _developerConsoleInput.TextLength;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Down && _developerConsoleHistory.Count > 0)
        {
            _developerConsoleHistoryIndex = Math.Min(_developerConsoleHistory.Count, _developerConsoleHistoryIndex + 1);
            _developerConsoleInput.Text = _developerConsoleHistoryIndex == _developerConsoleHistory.Count
                ? string.Empty
                : _developerConsoleHistory[_developerConsoleHistoryIndex];
            _developerConsoleInput.SelectionStart = _developerConsoleInput.TextLength;
            e.SuppressKeyPress = true;
        }
    }

    private async Task ExecuteDeveloperCommandAsync(string commandLine)
    {
        AppendConsoleLine($"neo++ > {commandLine}");
        string command = commandLine.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
        string argument = commandLine.Length > command.Length ? commandLine[command.Length..].Trim().Trim('"') : string.Empty;
        switch (command)
        {
            case "help":
                AppendConsoleLine("Commands: help, clear, version, run [file.neo], build [file.neo], disassemble [file.neo]");
                break;
            case "clear":
                _developerConsoleOutput.Clear();
                break;
            case "version":
                AppendConsoleLine($"Neo++ {NeoRuntime.Version} | NVM bytecode runtime");
                break;
            case "run":
                await RunConsoleFileAsync(argument);
                break;
            case "build":
            case "disassemble":
                await BuildConsoleFileAsync(command, argument);
                break;
            default:
                AppendConsoleLine($"Unknown command: {command}. Type help for available commands.");
                break;
        }
    }

    private async Task RunConsoleFileAsync(string argument)
    {
        if (argument.Length == 0)
        {
            await RunConsoleSourceAsync(_editor.Text, _currentFilePath);
            return;
        }

        try
        {
            string sourcePath = ResolveConsolePath(argument);
            string source = await File.ReadAllTextAsync(sourcePath);
            await RunConsoleSourceAsync(source, sourcePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            AppendConsoleLine(exception.Message);
        }
    }

    private async Task RunConsoleSourceAsync(string source, string? sourcePath)
    {
        try
        {
            var result = await Task.Run(() => new NeoRuntime().Run(source, sourcePath));
            AppendConsoleLine(result.Output.TrimEnd());
            AppendConsoleLine($"Completed in {result.Elapsed.TotalMilliseconds:F1} ms.");
            _status.Text = $"NVM finished in {result.Elapsed.TotalMilliseconds:F1} ms";
        }
        catch (Exception exception) when (exception is NeoLanguageException or InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            AppendConsoleLine(exception.Message);
            _status.Text = "Neo++ compile/runtime error";
        }
    }

    private async Task BuildConsoleFileAsync(string command, string argument)
    {
        try
        {
            string? sourcePath = argument.Length == 0 ? _currentFilePath : ResolveConsolePath(argument);
            string source = sourcePath is null ? _editor.Text : await File.ReadAllTextAsync(sourcePath);
            string bytecode = await Task.Run(() => new NeoRuntime().Disassemble(source));
            AppendConsoleLine(command == "build" ? "Compilation succeeded. NVM bytecode:" : "NVM bytecode:");
            AppendConsoleLine(bytecode.TrimEnd());
            _status.Text = "Neo++ source compiled to NVM bytecode";
        }
        catch (Exception exception) when (exception is NeoLanguageException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            AppendConsoleLine(exception.Message);
            _status.Text = "Neo++ compile error";
        }
    }

    private string ResolveConsolePath(string path)
    {
        string baseDirectory = _currentFilePath is null
            ? Environment.CurrentDirectory
            : Path.GetDirectoryName(_currentFilePath)!;
        return Path.GetFullPath(path, baseDirectory);
    }

    private void AppendConsoleLine(string text)
    {
        if (text.Length > 0)
        {
            _developerConsoleOutput.AppendText(text + Environment.NewLine);
        }
        _developerConsoleOutput.SelectionStart = _developerConsoleOutput.TextLength;
        _developerConsoleOutput.ScrollToCaret();
    }

    private void BuildProfile(TabPage page)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(30),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        panel.Controls.Add(Heading("Local player profile"));
        panel.Controls.Add(new Label
        {
            Text = "Offline player profiles and scores are stored on this PC only.",
            ForeColor = Color.FromArgb(173, 182, 199),
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 20)
        });
        _profileSummary.AutoSize = true;
        _profileSummary.Font = new Font("Segoe UI", 12F);
        _profileSummary.ForeColor = Color.White;
        panel.Controls.Add(_profileSummary);
        panel.Controls.Add(StyledButton("Add / switch local player", (_, _) => ChangePlayerName()));
        panel.Controls.Add(new Label
        {
            Text = "Code runs locally on your computer. Only run code you trust.",
            ForeColor = Color.FromArgb(255, 188, 105),
            AutoSize = true,
            Margin = new Padding(0, 24, 0, 0)
        });
        page.Controls.Add(panel);
    }

    private void BuildLeaderboard(TabPage page)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.FromArgb(24, 27, 36)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(Heading("Local leaderboard"), 0, 0);

        _leaderboard.Dock = DockStyle.Fill;
        _leaderboard.ReadOnly = true;
        _leaderboard.AllowUserToAddRows = false;
        _leaderboard.AllowUserToDeleteRows = false;
        _leaderboard.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _leaderboard.BackgroundColor = Color.FromArgb(17, 19, 26);
        _leaderboard.ForeColor = Color.FromArgb(30, 32, 40);
        _leaderboard.RowHeadersVisible = false;
        _leaderboard.Columns.Add("Rank", "Rank");
        _leaderboard.Columns.Add("Player", "Player");
        _leaderboard.Columns.Add("Solved", "Solved");
        _leaderboard.Columns.Add("Points", "Points");
        _leaderboard.Columns.Add("LastActive", "Last active");
        panel.Controls.Add(_leaderboard, 0, 1);
        page.Controls.Add(panel);
    }

    private void LoadChallenges()
    {
        _challenges.AddRange(
        [
            new Challenge
            {
                Id = "sum-one-to-ten", Title = "Sum 1..10", Category = "Math", Difficulty = "Easy",
                Description = "Print the sum of every integer from 1 through 10.",
                StarterCode = """
                    let total = 0;
                    loop i from 1 to 10 {
                        total += i;
                    }
                    print total;
                    """,
                ExpectedOutput = "55"
            },
            new Challenge
            {
                Id = "max-array", Title = "Find the maximum", Category = "Arrays", Difficulty = "Easy",
                Description = "Find and print the largest number in the given array.",
                StarterCode = """
                    let values = [4, 9, 2, 17, 5];
                    let max = values[0];
                    for (let i = 1; i < values.length; i++) {
                        if (values[i] > max) {
                            max = values[i];
                        }
                    }
                    print max;
                    """,
                ExpectedOutput = "17"
            },
            new Challenge
            {
                Id = "prime-check", Title = "Prime check", Category = "Math", Difficulty = "Medium",
                Description = "Print whether 29 is prime.",
                StarterCode = """
                    let n = 29;
                    let isPrime = true;
                    for (let i = 2; i * i <= n; i++) {
                        if (n % i == 0) {
                            isPrime = false;
                        }
                    }
                    if (isPrime) {
                        print "prime";
                    } else {
                        print "not prime";
                    }
                    """,
                ExpectedOutput = "prime"
            },
            new Challenge
            {
                Id = "fizz-buzz", Title = "FizzBuzz", Category = "Loops", Difficulty = "Medium",
                Description = "Print FizzBuzz for numbers 1 through 15, one per line.",
                StarterCode = """
                    for (let i = 1; i <= 15; i++) {
                        if (i % 15 == 0) {
                            print "FizzBuzz";
                        } else if (i % 3 == 0) {
                            print "Fizz";
                        } else if (i % 5 == 0) {
                            print "Buzz";
                        } else {
                            print i;
                        }
                    }
                    """,
                ExpectedOutput = """
                    1
                    2
                    Fizz
                    4
                    Buzz
                    Fizz
                    7
                    8
                    Fizz
                    Buzz
                    11
                    Fizz
                    13
                    14
                    FizzBuzz
                    """
            },
            new Challenge
            {
                Id = "reverse-string", Title = "Reverse a string", Category = "Strings", Difficulty = "Easy",
                Description = "Print the input word backwards.",
                StarterCode = """
                    let word = "neoplus";
                    let reversed = "";
                    loop i from 0 to word.length - 1 {
                        reversed = word[i] + reversed;
                    }
                    print reversed;
                    """,
                ExpectedOutput = "sulpoen"
            },
            new Challenge
            {
                Id = "longest-unique-substring", Title = "Longest unique substring", Category = "Strings", Difficulty = "Hard",
                Description = "Find the length of the longest substring without repeated characters.",
                StarterCode = """
                    let text = "abcabcbb";
                    let longest = 0;
                    for (let start = 0; start < text.length; start++) {
                        let seen = "";
                        for (let end = start; end < text.length; end++) {
                            let character = text[end];
                            if (contains(seen, character)) {
                                break;
                            }
                            seen += character;
                            if (seen.length > longest) {
                                longest = seen.length;
                            }
                        }
                    }
                    print longest;
                    """,
                ExpectedOutput = "3"
            }
        ]);
    }

    private void RefreshChallengeList()
    {
        if (_challengeList.IsDisposed)
        {
            return;
        }

        string selectedId = _challengeList.SelectedItem is Challenge selected ? selected.Id : string.Empty;
        _challengeList.BeginUpdate();
        _challengeList.Items.Clear();
        var filtered = _challenges.Where(challenge =>
            (_categoryFilter.SelectedIndex <= 0 || challenge.Category == _categoryFilter.SelectedItem?.ToString()) &&
            (_difficultyFilter.SelectedIndex <= 0 || challenge.Difficulty == _difficultyFilter.SelectedItem?.ToString())).ToList();
        foreach (var challenge in filtered)
        {
            _challengeList.Items.Add(challenge);
        }
        _challengeList.DisplayMember = nameof(Challenge.Title);
        _challengeList.EndUpdate();

        int restored = filtered.FindIndex(challenge => challenge.Id == selectedId);
        if (filtered.Count > 0)
        {
            _challengeList.SelectedIndex = restored >= 0 ? restored : 0;
        }
    }

    private void LoadSelectedChallenge()
    {
        if (_challengeList.SelectedItem is not Challenge challenge)
        {
            return;
        }

        _editor.Text = challenge.StarterCode;
        _currentFilePath = null;
        _loadedChallengeId = challenge.Id;
        _output.Text = $"{challenge.Title} | {challenge.Category} | {challenge.Difficulty}{Environment.NewLine}{challenge.Description}{Environment.NewLine}Expected output:{Environment.NewLine}{challenge.ExpectedOutput}";
        _status.Text = $"Loaded {challenge.Title}";
    }

    private async Task RunNeoProgramAsync(bool submit)
    {
        Challenge? challenge = _loadedChallengeId is null
            ? null
            : _challenges.FirstOrDefault(item => item.Id == _loadedChallengeId);
        if (submit && challenge is null)
        {
            _output.Text = "Select a challenge before submitting a solution.";
            return;
        }

        _runButton.Enabled = false;
        _status.Text = "Compiling Neo++ bytecode on NVM…";
        _output.Text = "Running Neo++ on the local NVM.";

        try
        {
            string source = _editor.Text;
            string? sourcePath = _currentFilePath;
            var result = await Task.Run(() => new NeoRuntime().Run(source, sourcePath));
            _output.Text = result.Output;
            if (submit && challenge is not null)
            {
                var expected = NormalizeOutput(challenge.ExpectedOutput);
                var actual = NormalizeOutput(result.Output);
                if (actual == expected)
                {
                    _output.AppendText($"{Environment.NewLine}PASS — output matched. Awarded {PointsFor(challenge)} points.");
                    _status.Text = $"Passed in {result.Elapsed.TotalMilliseconds:F1} ms";
                    _profileStore.RecordSolved(_profile, challenge);
                    _profile = _profileStore.Load();
                    RefreshProfileViews();
                }
                else
                {
                    _output.AppendText($"{Environment.NewLine}FAIL — expected:{Environment.NewLine}{expected}");
                    _status.Text = "Output did not match challenge";
                }
            }
            else _status.Text = $"NVM finished in {result.Elapsed.TotalMilliseconds:F1} ms";
        }
        catch (Exception exception) when (exception is NeoLanguageException or InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            _output.Text = exception.Message;
            _status.Text = "Neo++ compile/runtime error";
        }
        finally
        {
            _runButton.Enabled = true;
        }
    }

    private static string NormalizeOutput(string output) =>
        string.Join('\n', output.Replace("\r\n", "\n", StringComparison.Ordinal).Trim().Split('\n').Select(line => line.TrimEnd()));

    private void ChangePlayerName()
    {
        using var dialog = new Form
        {
            Text = "Player name",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ClientSize = new Size(360, 125),
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(24, 27, 36),
            ForeColor = Color.White
        };
        var nameBox = new TextBox { Text = string.Empty, Left = 18, Top = 18, Width = 320 };
        var save = new Button { Text = "Save", Left = 258, Top = 64, Width = 80, DialogResult = DialogResult.OK };
        dialog.Controls.Add(nameBox);
        dialog.Controls.Add(save);
        dialog.AcceptButton = save;
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            _profile = _profileStore.SelectOrCreate(nameBox.Text.Trim());
            RefreshProfileViews();
        }
    }

    private void RefreshProfileViews()
    {
        var sorted = _profileStore.LoadLeaderboard();
        _profileSummary.Text = $"Player: {_profile.DisplayName}{Environment.NewLine}Solved: {_profile.SolvedChallengeIds.Count}{Environment.NewLine}Points: {_profile.Points}";
        _leaderboard.Rows.Clear();
        int rank = 1;
        foreach (var entry in sorted.OrderByDescending(entry => entry.Points).ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            _leaderboard.Rows.Add(rank++, entry.DisplayName, entry.SolvedChallengeIds.Count, entry.Points, entry.LastActive.ToLocalTime().ToString("yyyy-MM-dd"));
        }
    }

    private static int PointsFor(Challenge challenge) => challenge.Difficulty switch
    {
        "Easy" => 100,
        "Medium" => 250,
        "Hard" => 500,
        _ => 100
    };

    private void NewFile()
    {
        _currentFilePath = null;
        _loadedChallengeId = null;
        _editor.Clear();
        _output.Clear();
        _status.Text = "New Neo++ source file";
    }

    private void InspectBytecode()
    {
        try
        {
            _output.Text = new NeoRuntime().Disassemble(_editor.Text);
            _status.Text = "Neo++ source compiled to NVM bytecode";
        }
        catch (NeoLanguageException exception)
        {
            _output.Text = exception.Message;
            _status.Text = "Neo++ compile error";
        }
    }

    private void OpenCodeFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Neo++ source (*.neo)|*.neo|All files (*.*)|*.*", DefaultExt = "neo" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _editor.Text = File.ReadAllText(dialog.FileName);
            _currentFilePath = dialog.FileName;
            _loadedChallengeId = null;
            _status.Text = "Opened " + Path.GetFileName(dialog.FileName);
        }
    }

    private void SaveCodeFile()
    {
        using var dialog = new SaveFileDialog { Filter = "Neo++ source (*.neo)|*.neo", DefaultExt = "neo", FileName = "main.neo" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _editor.Text);
            _currentFilePath = dialog.FileName;
            _loadedChallengeId = null;
            _status.Text = "Saved " + Path.GetFileName(dialog.FileName);
        }
    }

    private void ChooseJavaFile()
    {
        using var dialog = new OpenFileDialog { Filter = "Java source (*.java)|*.java|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _javaSourcePath.Text = dialog.FileName;
        }
    }

    private async Task RunJavaFileAsync()
    {
        if (string.IsNullOrWhiteSpace(_javaSourcePath.Text))
        {
            _javaOutput.Text = "Choose a .java source file first.";
            return;
        }

        _javaOutput.Text = "Starting Java source launcher…";
        try
        {
            string sourcePath = _javaSourcePath.Text;
            var result = await Task.Run(() => NeoJavaBridge.RunSource(sourcePath, [], Path.GetDirectoryName(sourcePath)!));
            _javaOutput.Text = result.Output;
            if (!string.IsNullOrWhiteSpace(result.Error)) _javaOutput.AppendText(Environment.NewLine + result.Error);
            _status.Text = result.ExitCode == 0 ? "Java bridge completed" : $"Java exited with code {result.ExitCode}";
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            _javaOutput.Text = exception.Message;
            _status.Text = "Java bridge unavailable";
        }
    }

    private void RefreshRuntimeSummary()
    {
        string javaStatus;
        try
        {
            var java = NeoJavaBridge.FindRuntime();
            javaStatus = $"{java.Version}{Environment.NewLine}Executable: {java.Executable}";
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            javaStatus = "Not detected — install a JDK 11+ and set JAVA_HOME or add java to PATH.";
        }

        _runtimeSummary.Text =
            $"NEO++ LANGUAGE   {NeoRuntime.Version}{Environment.NewLine}" +
            $"NVM              Bytecode compiler + stack virtual machine{Environment.NewLine}" +
            $"LANGUAGE FEATURES let / const, functions, recursion, arrays, switch, loops, break / continue{Environment.NewLine}" +
            $"BUILT-INS        print, len(), contains(), java(){Environment.NewLine}" +
            $"JAVA BRIDGE      {javaStatus}{Environment.NewLine}" +
            $"HUB              Offline challenges: {_challenges.Count} | Profile: {_profile.DisplayName}{Environment.NewLine}" +
            $"RUNTIME          .NET {Environment.Version} | {System.Runtime.InteropServices.RuntimeInformation.OSDescription}";
    }

    private static void StyleFilter(ComboBox combo)
    {
        combo.Dock = DockStyle.Fill;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = Color.FromArgb(18, 21, 29);
        combo.ForeColor = Color.White;
        combo.FlatStyle = FlatStyle.Flat;
    }

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 12, FontStyle.Bold),
        Margin = new Padding(2, 5, 8, 5)
    };

    private static Button StyledButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text };
        StyleButton(button, Color.FromArgb(57, 65, 83));
        button.Click += handler;
        return button;
    }

    private static void StyleButton(Button button, Color color)
    {
        button.AutoSize = true;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = color;
        button.ForeColor = Color.White;
        button.Padding = new Padding(10, 5, 10, 5);
        button.Margin = new Padding(8, 0, 0, 0);
    }
}
