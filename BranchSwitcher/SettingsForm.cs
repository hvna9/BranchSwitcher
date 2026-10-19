using System.ComponentModel;
using BranchSwitcher.Models;
using BranchSwitcher.Services;

namespace BranchSwitcher;

public sealed class SettingsForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly TextBox _gitBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _configsBox = new() { Dock = DockStyle.Fill };
    private readonly BindingList<RepoRow> _rows = new();
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = true,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    public SettingsForm(SettingsService settingsService)
    {
        _settingsService = settingsService;
        Text = "Impostazioni Branch Switcher";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 520);
        Size = new Size(900, 600);

        BuildUi();
        LoadSettings();
    }

    private void BuildUi()
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Alias repository", DataPropertyName = nameof(RepoRow.Alias), FillWeight = 30 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Path completo repository", DataPropertyName = nameof(RepoRow.Path), FillWeight = 70 });

        var browseColumn = new DataGridViewButtonColumn
        {
            HeaderText = "",
            Text = "Sfoglia...",
            UseColumnTextForButtonValue = true,
            FillWeight = 15
        };
        _grid.Columns.Add(browseColumn);
        _grid.CellContentClick += GridCellContentClick;

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        form.Controls.Add(new Label { Text = "Eseguibile Git:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 0, 0);
        form.Controls.Add(_gitBox, 1, 0);
        form.Controls.Add(new Label { Text = "Directory configs:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) }, 0, 1);
        form.Controls.Add(_configsBox, 1, 1);

        var group = new GroupBox { Text = "Repository", Dock = DockStyle.Fill, Padding = new Padding(8) };
        group.Controls.Add(_grid);
        form.SetColumnSpan(group, 2);
        form.Controls.Add(group, 0, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        var save = new Button { Text = "Salva", AutoSize = true };
        var cancel = new Button { Text = "Annulla", AutoSize = true };
        save.Click += (_, _) => SaveSettings();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        form.SetColumnSpan(buttons, 2);
        form.Controls.Add(buttons, 0, 3);

        Controls.Add(form);
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Load();
        _gitBox.Text = settings.GitExecutable;
        _configsBox.Text = settings.ConfigsDirectory;
        _rows.Clear();
        foreach (var item in settings.Repositories.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            _rows.Add(new RepoRow { Alias = item.Key, Path = item.Value });

        _grid.DataSource = _rows;
    }

    private void SaveSettings()
    {
        _grid.EndEdit();
        var repositories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in _rows)
        {
            if (string.IsNullOrWhiteSpace(row.Alias) && string.IsNullOrWhiteSpace(row.Path))
                continue;

            if (string.IsNullOrWhiteSpace(row.Alias) || string.IsNullOrWhiteSpace(row.Path))
            {
                MessageBox.Show("Ogni repository deve avere sia alias sia path.", "Impostazioni", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            repositories[row.Alias.Trim()] = row.Path.Trim();
        }

        var settings = new AppSettings
        {
            GitExecutable = string.IsNullOrWhiteSpace(_gitBox.Text) ? "git" : _gitBox.Text.Trim(),
            ConfigsDirectory = string.IsNullOrWhiteSpace(_configsBox.Text) ? "configs" : _configsBox.Text.Trim(),
            Repositories = repositories
        };

        _settingsService.Save(settings);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void GridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != 2)
            return;

        using var dialog = new FolderBrowserDialog
        {
            Description = "Seleziona la directory del repository Git",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        var currentValue = _grid.Rows[e.RowIndex].Cells[1].Value?.ToString();
        if (!string.IsNullOrWhiteSpace(currentValue) && Directory.Exists(currentValue))
            dialog.InitialDirectory = currentValue;

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _grid.Rows[e.RowIndex].Cells[1].Value = dialog.SelectedPath;
    }

    private sealed class RepoRow
    {
        public string Alias { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
