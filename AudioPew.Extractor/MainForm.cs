using System.Diagnostics;

namespace AudioPew.Extractor;

public partial class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly FfmpegRunner _ffmpeg;

    private readonly ListBox _fileList = new();
    private readonly ComboBox _presetCombo = new();
    private readonly ComboBox _outputModeCombo = new();
    private readonly TextBox _customFolderText = new();
    private readonly Button _browseButton = new();
    private readonly Button _extractButton = new();
    private readonly Button _openOutputButton = new();
    private readonly ProgressBar _progressBar = new();
    private readonly TextBox _logBox = new();
    private readonly Label _statusLabel = new();
    private string _lastOutputFolder = string.Empty;

    public MainForm()
    {
        _settings = AppSettings.Load();
        _ffmpeg = new FfmpegRunner();

        InitializeUi();
        LoadSettingsIntoUi();
        UpdateFfmpegStatus();
    }

    private void InitializeUi()
    {
        Text = "AudioPew";
        Width = 860;
        Height = 650;
        MinimumSize = new Size(780, 560);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        Theme.ApplyForm(this);

        DragEnter += MainForm_DragEnter;
        DragDrop += MainForm_DragDrop;
        FormClosing += (_, _) => SaveSettingsFromUi();

        var header = new Panel { Dock = DockStyle.Top, Height = 64 };
        Theme.ApplyPanel(header);

        var title = new Label
        {
            Text = "AudioPew",
            Left = 18,
            Top = 10,
            Width = 300,
            Height = 26,
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };
        header.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Simple portable FFmpeg audio extraction",
            Left = 20,
            Top = 38,
            Width = 420,
            Height = 18,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            ForeColor = Theme.MutedText,
            BackColor = Color.Transparent
        };
        header.Controls.Add(subtitle);

        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            BackColor = Theme.WindowBg
        };

        // Important: add fill panel first, then header last,
        // so the docked header stays visually on top.
        Controls.Add(content);
        Controls.Add(header);

        var filesPanel = CreateSectionPanel(16, 16, 500, 292, "Files");
        content.Controls.Add(filesPanel);

        _fileList.Left = 16;
        _fileList.Top = 42;
        _fileList.Width = 468;
        _fileList.Height = 176;
        _fileList.AllowDrop = true;
        _fileList.HorizontalScrollbar = true;
        Theme.ApplyInput(_fileList);
        _fileList.DragEnter += MainForm_DragEnter;
        _fileList.DragDrop += MainForm_DragDrop;
        filesPanel.Controls.Add(_fileList);

        var addFiles = new Button { Text = "Add Files", Left = 16, Top = 246, Width = 110 };
        Theme.ApplyButton(addFiles);
        addFiles.Click += (_, _) => AddFilesFromDialog();
        filesPanel.Controls.Add(addFiles);

        var removeSelected = new Button { Text = "Remove", Left = 136, Top = 246, Width = 110 };
        Theme.ApplyButton(removeSelected);
        removeSelected.Click += (_, _) => RemoveSelectedFiles();
        filesPanel.Controls.Add(removeSelected);

        var clearFiles = new Button { Text = "Clear", Left = 256, Top = 246, Width = 110 };
        Theme.ApplyButton(clearFiles);
        clearFiles.Click += (_, _) => _fileList.Items.Clear();
        filesPanel.Controls.Add(clearFiles);

        var dropHint = Theme.Label("Drag MP4 / MOV / MTS / MKV files into the list.", 16, 222, 420, 18, true);
        filesPanel.Controls.Add(dropHint);

        var optionsPanel = CreateSectionPanel(532, 16, 280, 292, "Output");
        content.Controls.Add(optionsPanel);

        optionsPanel.Controls.Add(Theme.Label("Format preset", 16, 42, 220));
        _presetCombo.Left = 16;
        _presetCombo.Top = 66;
        _presetCombo.Width = 248;
        _presetCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        Theme.ApplyInput(_presetCombo);
        foreach (AudioPreset preset in AudioPreset.All)
            _presetCombo.Items.Add(preset);
        optionsPanel.Controls.Add(_presetCombo);

        optionsPanel.Controls.Add(Theme.Label("Output folder", 16, 108, 220));
        _outputModeCombo.Left = 16;
        _outputModeCombo.Top = 132;
        _outputModeCombo.Width = 248;
        _outputModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _outputModeCombo.Items.Add("Same folder as source");
        _outputModeCombo.Items.Add("Custom folder");
        Theme.ApplyInput(_outputModeCombo);
        _outputModeCombo.SelectedIndexChanged += (_, _) => UpdateCustomFolderEnabled();
        optionsPanel.Controls.Add(_outputModeCombo);

        _customFolderText.Left = 16;
        _customFolderText.Top = 174;
        _customFolderText.Width = 168;
        _customFolderText.PlaceholderText = "Custom folder...";
        Theme.ApplyInput(_customFolderText);
        optionsPanel.Controls.Add(_customFolderText);

        _browseButton.Text = "Browse";
        _browseButton.Left = 194;
        _browseButton.Top = 172;
        _browseButton.Width = 70;
        Theme.ApplyButton(_browseButton);
        _browseButton.Click += (_, _) => BrowseForOutputFolder();
        optionsPanel.Controls.Add(_browseButton);

        _extractButton.Text = "Extract Audio";
        _extractButton.Left = 16;
        _extractButton.Top = 226;
        _extractButton.Width = 248;
        Theme.ApplyButton(_extractButton, primary: true);
        _extractButton.Click += async (_, _) => await ExtractAllAsync();
        optionsPanel.Controls.Add(_extractButton);

        var progressPanel = CreateSectionPanel(16, 326, 796, 250, "Progress / Log");
        content.Controls.Add(progressPanel);

        _statusLabel.Left = 16;
        _statusLabel.Top = 42;
        _statusLabel.Width = 620;
        _statusLabel.Height = 22;
        _statusLabel.Text = "Ready";
        _statusLabel.ForeColor = Theme.MutedText;
        progressPanel.Controls.Add(_statusLabel);

        _progressBar.Left = 16;
        _progressBar.Top = 70;
        _progressBar.Width = 760;
        _progressBar.Height = 22;
        progressPanel.Controls.Add(_progressBar);

        _logBox.Left = 16;
        _logBox.Top = 106;
        _logBox.Width = 760;
        _logBox.Height = 88;
        _logBox.Multiline = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.ReadOnly = true;
        Theme.ApplyInput(_logBox);
        progressPanel.Controls.Add(_logBox);

        _openOutputButton.Text = "Open Output Folder";
        _openOutputButton.Left = 16;
        _openOutputButton.Top = 204;
        _openOutputButton.Width = 160;
        _openOutputButton.Enabled = false;
        Theme.ApplyButton(_openOutputButton);
        _openOutputButton.Click += (_, _) => OpenLastOutputFolder();
        progressPanel.Controls.Add(_openOutputButton);
    }

    private Panel CreateSectionPanel(int x, int y, int width, int height, string title)
    {
        var panel = new Panel
        {
            Left = x,
            Top = y,
            Width = width,
            Height = height,
            BackColor = Theme.PanelBg,
            BorderStyle = BorderStyle.FixedSingle
        };

        var label = new Label
        {
            Text = title,
            Left = 14,
            Top = 10,
            Width = width - 28,
            Height = 22,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            ForeColor = Theme.Text,
            BackColor = Color.Transparent
        };
        panel.Controls.Add(label);

        return panel;
    }

    private void LoadSettingsIntoUi()
    {
        _presetCombo.SelectedIndex = Math.Clamp(_settings.SelectedPresetIndex, 0, AudioPreset.All.Count - 1);
        _outputModeCombo.SelectedItem = _settings.OutputMode;
        if (_outputModeCombo.SelectedIndex < 0)
            _outputModeCombo.SelectedIndex = 0;

        _customFolderText.Text = _settings.CustomOutputFolder;
        UpdateCustomFolderEnabled();
    }

    private void SaveSettingsFromUi()
    {
        _settings.SelectedPresetIndex = Math.Max(0, _presetCombo.SelectedIndex);
        _settings.OutputMode = _outputModeCombo.SelectedItem?.ToString() ?? "Same folder as source";
        _settings.CustomOutputFolder = _customFolderText.Text.Trim();
        _settings.Save();
    }

    private void UpdateFfmpegStatus()
    {
        if (_ffmpeg.IsAvailable)
        {
            Log("ffmpeg found.");
        }
        else
        {
            Log("ffmpeg.exe not found. Place it here: Tools\\ffmpeg.exe");
            _statusLabel.Text = "Missing Tools\\ffmpeg.exe";
            _statusLabel.ForeColor = Theme.Warning;
        }
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            AddFiles(paths);
    }

    private void AddFilesFromDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select video files",
            Filter = "Video files|*.mp4;*.mov;*.mts;*.m2ts;*.mkv;*.avi;*.wmv|All files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddFiles(dialog.FileNames);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mov", ".mts", ".m2ts", ".mkv", ".avi", ".wmv"
        };

        foreach (string path in paths)
        {
            if (!File.Exists(path))
                continue;

            if (!supported.Contains(Path.GetExtension(path)))
                continue;

            if (!_fileList.Items.Cast<string>().Contains(path, StringComparer.OrdinalIgnoreCase))
                _fileList.Items.Add(path);
        }

        _statusLabel.Text = $"{_fileList.Items.Count} file(s) ready.";
        _statusLabel.ForeColor = Theme.MutedText;
    }

    private void RemoveSelectedFiles()
    {
        var selected = _fileList.SelectedItems.Cast<object>().ToList();
        foreach (object item in selected)
            _fileList.Items.Remove(item);
    }

    private void UpdateCustomFolderEnabled()
    {
        bool custom = _outputModeCombo.SelectedItem?.ToString() == "Custom folder";

        _customFolderText.Enabled = true;
        _customFolderText.ReadOnly = !custom;
        _customFolderText.BackColor = custom ? Theme.InputBg : Theme.WindowBg;
        _customFolderText.ForeColor = custom ? Theme.Text : Theme.MutedText;

        _browseButton.Enabled = custom;
    }

    private void BrowseForOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose where extracted audio files should be saved"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _customFolderText.Text = dialog.SelectedPath;
    }

    private async Task ExtractAllAsync()
    {
        if (_fileList.Items.Count == 0)
        {
            MessageBox.Show(this, "Add one or more video files first.", "AudioPew", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!_ffmpeg.IsAvailable)
        {
            MessageBox.Show(this, "ffmpeg.exe is missing. Place it in the Tools folder next to AudioPew.exe.", "AudioPew", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_presetCombo.SelectedItem is not AudioPreset preset)
            return;

        SaveSettingsFromUi();
        ToggleWorkingState(true);
        _progressBar.Value = 0;
        _openOutputButton.Enabled = false;

        var files = _fileList.Items.Cast<string>().ToList();

        try
        {
            for (int i = 0; i < files.Count; i++)
            {
                string input = files[i];
                string outputFolder = ResolveOutputFolder(input);
                string output = _ffmpeg.BuildOutputPath(input, preset, outputFolder);
                _lastOutputFolder = Path.GetDirectoryName(output) ?? outputFolder;

                _statusLabel.Text = $"Extracting {i + 1} of {files.Count}: {Path.GetFileName(input)}";
                _statusLabel.ForeColor = Theme.Text;
                Log($"\r\nExtracting: {input}");
                Log($"Output: {output}");

                int exitCode = await _ffmpeg.ExtractAsync(input, output, preset, Log);
                if (exitCode != 0)
                {
                    _statusLabel.Text = $"FFmpeg failed on: {Path.GetFileName(input)}";
                    _statusLabel.ForeColor = Theme.Error;
                    Log($"FAILED with exit code {exitCode}");
                    break;
                }

                _progressBar.Value = (int)Math.Round(((i + 1) / (double)files.Count) * 100);
                Log("Done.");
            }

            if (_progressBar.Value == 100)
            {
                _statusLabel.Text = "Extraction complete.";
                _statusLabel.ForeColor = Theme.Success;
            }
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Extraction failed.";
            _statusLabel.ForeColor = Theme.Error;
            Log(ex.ToString());
        }
        finally
        {
            ToggleWorkingState(false);
            _openOutputButton.Enabled = Directory.Exists(_lastOutputFolder);
        }
    }

    private string ResolveOutputFolder(string inputPath)
    {
        bool custom = _outputModeCombo.SelectedItem?.ToString() == "Custom folder";
        if (custom && !string.IsNullOrWhiteSpace(_customFolderText.Text))
            return _customFolderText.Text.Trim();

        return Path.GetDirectoryName(inputPath) ?? AppContext.BaseDirectory;
    }

    private void ToggleWorkingState(bool working)
    {
        _extractButton.Enabled = !working;
        _presetCombo.Enabled = !working;
        _outputModeCombo.Enabled = !working;
        _fileList.Enabled = !working;
        UpdateCustomFolderEnabled();
        if (working)
        {
            _customFolderText.ReadOnly = true;
            _customFolderText.BackColor = Theme.WindowBg;
            _customFolderText.ForeColor = Theme.MutedText;
            _browseButton.Enabled = false;
        }
    }

    private void OpenLastOutputFolder()
    {
        if (Directory.Exists(_lastOutputFolder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _lastOutputFolder,
                UseShellExecute = true
            });
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Log(message));
            return;
        }

        _logBox.AppendText(message + Environment.NewLine);
    }
}
