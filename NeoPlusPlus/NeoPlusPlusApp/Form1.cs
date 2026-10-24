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
    private readonly Label _status = new();
    private readonly Label _profileSummary = new();
    private readonly DataGridView _leaderboard = new();
    private readonly Button _runButton = new();
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
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(14),
            BackColor = Color.FromArgb(17, 19, 26)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

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

        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
        var arena = new TabPage("Arena") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var profile = new TabPage("Profile") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        var leaderboard = new TabPage("Leaderboard") { BackColor = Color.FromArgb(24, 27, 36), ForeColor = Color.White };
        BuildArena(arena);
        BuildProfile(profile);
        BuildLeaderboard(leaderboard);
        tabs.TabPages.AddRange([arena, profile, leaderboard]);

        _status.Text = "Ready";
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = Color.FromArgb(145, 220, 166);
        _status.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        root.Controls.Add(_status, 0, 2);
        Controls.Add(root);
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
        toolbar.Controls.Add(Heading("C# Editor"));
        toolbar.Controls.Add(StyledButton("Open", (_, _) => OpenCodeFile()));
        toolbar.Controls.Add(StyledButton("Save", (_, _) => SaveCodeFile()));
        _runButton.Text = "Run & Judge";
        StyleButton(_runButton, Color.FromArgb(69, 130, 246));
        _runButton.Click += async (_, _) => await RunChallengeAsync();
        toolbar.Controls.Add(_runButton);
        editorPanel.Controls.Add(toolbar, 0, 0);

        _editor.Dock = DockStyle.Fill;
        _editor.BackColor = Color.FromArgb(15, 17, 23);
        _editor.ForeColor = Color.FromArgb(225, 232, 246);
        _editor.Font = new Font("Consolas", 11F);
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
        _output.Font = new Font("Consolas", 10F);
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
                    int total = 0;
                    for (int i = 1; i <= 10; i++)
                    {
                        total += i;
                    }
                    Console.WriteLine(total);
                    """,
                ExpectedOutput = "55"
            },
            new Challenge
            {
                Id = "max-array", Title = "Find the maximum", Category = "Arrays", Difficulty = "Easy",
                Description = "Find and print the largest number in the given array.",
                StarterCode = """
                    int[] values = { 4, 9, 2, 17, 5 };
                    int max = values[0];
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (values[i] > max) max = values[i];
                    }
                    Console.WriteLine(max);
                    """,
                ExpectedOutput = "17"
            },
            new Challenge
            {
                Id = "prime-check", Title = "Prime check", Category = "Math", Difficulty = "Medium",
                Description = "Print whether 29 is prime.",
                StarterCode = """
                    int n = 29;
                    bool isPrime = true;
                    for (int i = 2; i * i <= n; i++)
                    {
                        if (n % i == 0) isPrime = false;
                    }
                    Console.WriteLine(isPrime ? "prime" : "not prime");
                    """,
                ExpectedOutput = "prime"
            },
            new Challenge
            {
                Id = "fizz-buzz", Title = "FizzBuzz", Category = "Loops", Difficulty = "Medium",
                Description = "Print FizzBuzz for numbers 1 through 15, one per line.",
                StarterCode = """
                    for (int i = 1; i <= 15; i++)
                    {
                        if (i % 15 == 0) Console.WriteLine("FizzBuzz");
                        else if (i % 3 == 0) Console.WriteLine("Fizz");
                        else if (i % 5 == 0) Console.WriteLine("Buzz");
                        else Console.WriteLine(i);
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
                    string word = "neoplus";
                    char[] chars = word.ToCharArray();
                    Array.Reverse(chars);
                    Console.WriteLine(new string(chars));
                    """,
                ExpectedOutput = "sulpoen"
            },
            new Challenge
            {
                Id = "longest-unique-substring", Title = "Longest unique substring", Category = "Strings", Difficulty = "Hard",
                Description = "Find the length of the longest substring without repeated characters.",
                StarterCode = """
                    string text = "abcabcbb";
                    int start = 0;
                    int longest = 0;
                    Dictionary<char, int> lastSeen = new();
                    for (int i = 0; i < text.Length; i++)
                    {
                        char current = text[i];
                        if (lastSeen.TryGetValue(current, out int previous) && previous >= start)
                        {
                            start = previous + 1;
                        }
                        lastSeen[current] = i;
                        longest = Math.Max(longest, i - start + 1);
                    }
                    Console.WriteLine(longest);
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
        _output.Text = $"{challenge.Title} | {challenge.Category} | {challenge.Difficulty}{Environment.NewLine}{challenge.Description}{Environment.NewLine}Expected output:{Environment.NewLine}{challenge.ExpectedOutput}";
        _status.Text = $"Loaded {challenge.Title}";
    }

    private async Task RunChallengeAsync()
    {
        if (_challengeList.SelectedItem is not Challenge challenge)
        {
            _output.Text = "Select a challenge first.";
            return;
        }

        _runButton.Enabled = false;
        _status.Text = "Compiling and running C#…";
        _output.Text = "Running locally. Only run code you trust.";

        try
        {
            var result = await CSharpRunner.RunAsync(_editor.Text, TimeSpan.FromSeconds(5));
            var actual = NormalizeOutput(result.StandardOutput);
            var expected = NormalizeOutput(challenge.ExpectedOutput);
            bool passed = result.ExitCode == 0 && !result.TimedOut && actual == expected;

            _output.Text = result.StandardOutput;
            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                _output.AppendText(Environment.NewLine + result.StandardError);
            }

            if (result.TimedOut)
            {
                _output.AppendText(Environment.NewLine + "Execution timed out after 5 seconds. The process was stopped.");
                _status.Text = "Timed out";
            }
            else if (passed)
            {
                _output.AppendText(Environment.NewLine + Environment.NewLine + "PASS — output matches the challenge.");
                _status.Text = "Challenge passed";
                _profileStore.RecordSolved(_profile, challenge);
                _profile = _profileStore.Load();
                RefreshProfileViews();
            }
            else
            {
                _output.AppendText($"{Environment.NewLine}{Environment.NewLine}FAIL — expected:{Environment.NewLine}{expected}{Environment.NewLine}Exit code: {result.ExitCode}");
                _status.Text = result.ExitCode == 0 ? "Output did not match" : "Compilation or runtime error";
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _output.Text = "Could not run solution: " + exception.Message;
            _status.Text = "Run failed";
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

    private void OpenCodeFile()
    {
        using var dialog = new OpenFileDialog { Filter = "C# source (*.cs)|*.cs|All files (*.*)|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _editor.Text = File.ReadAllText(dialog.FileName);
            _status.Text = "Opened " + Path.GetFileName(dialog.FileName);
        }
    }

    private void SaveCodeFile()
    {
        using var dialog = new SaveFileDialog { Filter = "C# source (*.cs)|*.cs", DefaultExt = "cs", FileName = "Solution.cs" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            File.WriteAllText(dialog.FileName, _editor.Text);
            _status.Text = "Saved " + Path.GetFileName(dialog.FileName);
        }
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
