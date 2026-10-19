using System.Diagnostics;
using BranchSwitcher.Models;
using BranchSwitcher.Services;

namespace BranchSwitcher;

public sealed class MainForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly ConfigService _configService;
    private readonly GitService _gitService;

    private readonly TextBox _searchBox = new() { Width = 190, PlaceholderText = "Cerca configurazione..." };
    private readonly ComboBox _taskCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
    private readonly ToolTip _taskToolTip = new() { ShowAlways = true };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        ReadOnly = true,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };
    private readonly RichTextBox _log = new() { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9F) };
    private readonly Button _applyButton = new() { Text = "Avvia switch", AutoSize = true };
    private readonly Button _newConfigButton = new() { Text = "Nuova configurazione", AutoSize = true };
    private readonly Button _deleteConfigButton = new() { Text = "Elimina configurazione", AutoSize = true };
    private readonly Button _reloadButton = new() { Text = "Aggiorna lista", AutoSize = true };
    private readonly Button _settingsButton = new() { Text = "Impostazioni", AutoSize = true };
    private readonly Button _openConfigsButton = new() { Text = "Apri configs", AutoSize = true };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = "Pronto" };

    private List<string> _allTasks = new();
    private bool _loadingTasks;

    public MainForm(SettingsService settingsService, ConfigService configService, GitService gitService)
    {
        _settingsService = settingsService;
        _configService = configService;
        _gitService = gitService;

        Text = "BranchSwitcher";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 680);
        Size = new Size(1250, 790);

        BuildUi();
        WireEvents();
        Shown += (_, _) => InitializeApplication();
    }

    private void BuildUi()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Repository", DataPropertyName = nameof(RowModel.Repository), FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Branch corrente", DataPropertyName = nameof(RowModel.CurrentBranch), FillWeight = 25 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Branch richiesto", DataPropertyName = nameof(RowModel.TargetBranch), FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Stato", DataPropertyName = nameof(RowModel.Status), FillWeight = 20 });

        var selectorsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(8),
            AutoSize = true
        };

        selectorsPanel.Controls.Add(new Label { Text = "Configurazione:", AutoSize = true, Margin = new Padding(3, 8, 6, 3) });
        selectorsPanel.Controls.Add(_taskCombo);
        selectorsPanel.Controls.Add(new Label { Text = "Ricerca:", AutoSize = true, Margin = new Padding(12, 8, 6, 3) });
        selectorsPanel.Controls.Add(_searchBox);
        selectorsPanel.Controls.Add(_reloadButton);
        selectorsPanel.Controls.Add(_newConfigButton);
        selectorsPanel.Controls.Add(_deleteConfigButton);
        selectorsPanel.Controls.Add(_openConfigsButton);
        selectorsPanel.Controls.Add(_settingsButton);
        selectorsPanel.Controls.Add(_applyButton);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 320,
            Panel1MinSize = 180,
            Panel2MinSize = 150
        };
        split.Panel1.Controls.Add(_grid);
        split.Panel2.Controls.Add(_log);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(selectorsPanel, 0, 0);
        root.Controls.Add(split, 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);

        Controls.Add(root);
    }

    private void WireEvents()
    {
        _taskCombo.SelectedIndexChanged += async (_, _) =>
        {
            UpdateConfigTooltip();
            if (!_loadingTasks)
                await LoadSelectedTaskAsync();
        };
        _taskCombo.MouseHover += (_, _) => ShowSelectedConfigTooltip();
        _taskCombo.Enter += (_, _) => ShowSelectedConfigTooltip();
        _taskCombo.DropDown += (_, _) => UpdateDropDownWidth();

        _searchBox.TextChanged += (_, _) => FilterTasks();
        _reloadButton.Click += (_, _) => LoadTasks();
        _newConfigButton.Click += (_, _) => CreateConfiguration();
        _deleteConfigButton.Click += (_, _) => DeleteSelectedConfiguration();
        _settingsButton.Click += (_, _) => OpenSettings();
        _openConfigsButton.Click += (_, _) => OpenConfigsFolder();
        _applyButton.Click += async (_, _) => await ApplySelectedTaskAsync();
    }

    private void InitializeApplication()
    {
        var settings = _settingsService.Load();
        if (settings.Repositories.Count == 0)
        {
            MessageBox.Show(
                "Prima configurazione: imposta i path dei repository nella schermata Impostazioni.",
                "Branch Switcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            OpenSettings();
        }

        LoadTasks();
    }

    private void LoadTasks(string? taskToSelect = null)
    {
        try
        {
            var selected = taskToSelect ?? _taskCombo.SelectedItem?.ToString();
            _allTasks = _configService.GetAvailableTasks().ToList();
            PopulateTaskCombo(_allTasks, selected);

            if (_allTasks.Count == 0)
            {
                _grid.DataSource = null;
                SetStatus("Nessuna configurazione .conf presente.");
                UpdateActionButtons();
                return;
            }

            SetStatus($"Caricate {_allTasks.Count} configurazioni.");
            UpdateActionButtons();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void FilterTasks()
    {
        var selected = _taskCombo.SelectedItem?.ToString();
        var filter = _searchBox.Text.Trim();
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allTasks
            : _allTasks.Where(x => x.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        PopulateTaskCombo(filtered, selected);

        if (filtered.Count == 0)
        {
            _grid.DataSource = null;
            _log.Clear();
            SetStatus($"Nessuna configurazione trovata per '{filter}'.");
        }
        else
        {
            SetStatus($"Visualizzate {filtered.Count} di {_allTasks.Count} configurazioni.");
        }

        UpdateActionButtons();
    }

    private void PopulateTaskCombo(IReadOnlyList<string> tasks, string? preferredSelection)
    {
        _loadingTasks = true;
        try
        {
            _taskCombo.BeginUpdate();
            _taskCombo.Items.Clear();
            _taskCombo.Items.AddRange(tasks.Cast<object>().ToArray());

            if (tasks.Count > 0)
            {
                var index = preferredSelection is null
                    ? 0
                    : tasks.ToList().FindIndex(x => x.Equals(preferredSelection, StringComparison.OrdinalIgnoreCase));
                _taskCombo.SelectedIndex = index >= 0 ? index : 0;
            }
            else
            {
                _taskCombo.SelectedIndex = -1;
            }
        }
        finally
        {
            _taskCombo.EndUpdate();
            _loadingTasks = false;
        }

        UpdateConfigTooltip();
        UpdateDropDownWidth();

        if (_taskCombo.SelectedItem is string)
            _ = LoadSelectedTaskAsync();
    }

    private async Task LoadSelectedTaskAsync(bool clearLog = true)
    {
        if (_taskCombo.SelectedItem is not string task)
            return;

        try
        {
            var entries = _configService.LoadTask(task);
            var settings = _settingsService.Load();
            var rows = new List<RowModel>();

            foreach (var entry in entries)
            {
                if (!settings.Repositories.TryGetValue(entry.Repository, out var path) || string.IsNullOrWhiteSpace(path))
                {
                    rows.Add(new RowModel(entry.Repository, "-", entry.Branch, "Path non configurato"));
                    continue;
                }

                var current = Directory.Exists(path)
                    ? await _gitService.GetCurrentBranchAsync(settings.GitExecutable, path)
                    : "-";

                var status = Directory.Exists(path) ? "Pronto" : "Directory non trovata";
                rows.Add(new RowModel(entry.Repository, current, entry.Branch, status));
            }

            _grid.DataSource = rows;
            if (clearLog)
            {
                _log.Clear();
                AppendLog($"Configurazione selezionata: {task}");
                foreach (var entry in entries)
                    AppendLog($"  {entry.Repository} -> {entry.Branch}");
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async Task ApplySelectedTaskAsync()
    {
        if (_taskCombo.SelectedItem is not string task)
            return;

        var confirm = MessageBox.Show(
            $"Avviare lo switch per la configurazione '{task}'?\n\nVerranno eseguiti fetch, switch e pull --ff-only sui repository indicati.",
            "Conferma switch",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes)
            return;

        var entries = _configService.LoadTask(task);
        var settings = _settingsService.Load();

        SetBusy(true);
        _log.Clear();
        SetStatus("Switch in corso...");

        var success = 0;
        var errors = 0;

        try
        {
            foreach (var entry in entries)
            {
                AppendLog(string.Empty);
                AppendLog($"=== {entry.Repository} -> {entry.Branch} ===");

                if (!settings.Repositories.TryGetValue(entry.Repository, out var repoPath) || string.IsNullOrWhiteSpace(repoPath))
                {
                    AppendLog("ERRORE: path repository non configurato.");
                    errors++;
                    continue;
                }

                var result = await _gitService.ApplyAsync(
                    settings.GitExecutable,
                    entry.Repository,
                    repoPath,
                    entry.Branch,
                    AppendLog);

                AppendLog((result.Success ? "OK: " : "ERRORE: ") + result.Message);
                if (result.Success) success++; else errors++;
            }

            AppendLog(string.Empty);
            AppendLog($"Completati: {success} | Errori/skip: {errors}");
            SetStatus(errors == 0 ? "Switch completato con successo." : $"Switch completato con {errors} errori/skip.");

            await LoadSelectedTaskAsync(clearLog: false);

            MessageBox.Show(
                errors == 0
                    ? $"Switch della configurazione '{task}' completato correttamente."
                    : $"Switch completato con {errors} errori/skip. Controlla il log.",
                "Risultato",
                MessageBoxButtons.OK,
                errors == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AppendLog("ERRORE IMPREVISTO: " + ex.Message);
            ShowError(ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CreateConfiguration()
    {
        var settings = _settingsService.Load();
        if (settings.Repositories.Count == 0)
        {
            MessageBox.Show(
                "Configura prima almeno un repository nelle Impostazioni.",
                "Nuova configurazione",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            OpenSettings();
            return;
        }

        using var dialog = new ConfigForm(_configService, _settingsService);
        if (dialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.CreatedTaskName))
        {
            _searchBox.Clear();
            LoadTasks(dialog.CreatedTaskName);
            AppendLog($"Configurazione '{dialog.CreatedTaskName}' salvata.");
        }
    }

    private void DeleteSelectedConfiguration()
    {
        if (_taskCombo.SelectedItem is not string task)
            return;

        var confirm = MessageBox.Show(
            $"Eliminare definitivamente la configurazione '{task}'?\n\nVerrà cancellato il relativo file .conf.",
            "Elimina configurazione",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _configService.DeleteTask(task);
            _searchBox.Clear();
            LoadTasks();
            AppendLog($"Configurazione '{task}' eliminata.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(_settingsService);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AppendLog("Impostazioni salvate.");
            LoadTasks();
        }
    }

    private void OpenConfigsFolder()
    {
        try
        {
            var path = _configService.GetConfigsDirectory();
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void UpdateConfigTooltip()
    {
        var fullName = _taskCombo.SelectedItem?.ToString() ?? "";
        _taskToolTip.SetToolTip(_taskCombo, fullName);
    }

    private void ShowSelectedConfigTooltip()
    {
        var fullName = _taskCombo.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(fullName))
            _taskToolTip.Show(fullName, _taskCombo, 0, _taskCombo.Height + 2, 3500);
    }

    private void UpdateDropDownWidth()
    {
        if (_taskCombo.Items.Count == 0)
            return;

        using var graphics = _taskCombo.CreateGraphics();
        var maxWidth = _taskCombo.Width;
        foreach (var item in _taskCombo.Items)
        {
            var width = (int)graphics.MeasureString(item?.ToString() ?? string.Empty, _taskCombo.Font).Width + 28;
            if (width > maxWidth)
                maxWidth = width;
        }

        _taskCombo.DropDownWidth = Math.Min(maxWidth, 900);
    }

    private void SetBusy(bool busy)
    {
        _applyButton.Enabled = !busy && _taskCombo.SelectedItem is string;
        _settingsButton.Enabled = !busy;
        _reloadButton.Enabled = !busy;
        _newConfigButton.Enabled = !busy;
        _deleteConfigButton.Enabled = !busy && _taskCombo.SelectedItem is string;
        _openConfigsButton.Enabled = !busy;
        _taskCombo.Enabled = !busy;
        _searchBox.Enabled = !busy;
    }

    private void UpdateActionButtons()
    {
        var hasSelection = _taskCombo.SelectedItem is string;
        _applyButton.Enabled = hasSelection;
        _deleteConfigButton.Enabled = hasSelection;
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    private void ShowError(string message)
    {
        SetStatus("Errore");
        MessageBox.Show(message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private sealed record RowModel(string Repository, string CurrentBranch, string TargetBranch, string Status);
}
