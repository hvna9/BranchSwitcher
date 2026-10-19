using System.ComponentModel;
using BranchSwitcher.Models;
using BranchSwitcher.Services;

namespace BranchSwitcher;

public sealed class ConfigForm : Form
{
    private readonly ConfigService _configService;
    private readonly SettingsService _settingsService;
    private readonly TextBox _nameBox = new() { Dock = DockStyle.Fill };
    private readonly BindingList<ConfigRow> _rows = new();
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = true,
        AllowUserToDeleteRows = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    public string? CreatedTaskName { get; private set; }

    public ConfigForm(ConfigService configService, SettingsService settingsService)
    {
        _configService = configService;
        _settingsService = settingsService;

        Text = "Nuova configurazione";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(700, 470);
        Size = new Size(820, 540);

        BuildUi();
    }

    private void BuildUi()
    {
        var aliases = _settingsService.Load().Repositories.Keys
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var repoColumn = new DataGridViewComboBoxColumn
        {
            HeaderText = "Repository",
            DataPropertyName = nameof(ConfigRow.Repository),
            FillWeight = 45,
            FlatStyle = FlatStyle.Flat
        };
        repoColumn.Items.AddRange(aliases.Cast<object>().ToArray());

        _grid.Columns.Add(repoColumn);
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Branch",
            DataPropertyName = nameof(ConfigRow.Branch),
            FillWeight = 55
        });
        _grid.DataSource = _rows;

        if (aliases.Count > 0)
            _rows.Add(new ConfigRow { Repository = aliases[0] });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(12)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Nome configurazione:",
            AutoSize = true,
            Margin = new Padding(3, 8, 3, 3)
        }, 0, 0);
        root.Controls.Add(_nameBox, 1, 0);

        var help = new Label
        {
            Text = "Aggiungi uno o più repository e indica il branch richiesto per ciascuno.",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 6, 3, 8)
        };
        root.SetColumnSpan(help, 2);
        root.Controls.Add(help, 0, 1);

        var group = new GroupBox { Text = "Repository della configurazione", Dock = DockStyle.Fill, Padding = new Padding(8) };
        group.Controls.Add(_grid);
        root.SetColumnSpan(group, 2);
        root.Controls.Add(group, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        var save = new Button { Text = "Salva configurazione", AutoSize = true };
        var cancel = new Button { Text = "Annulla", AutoSize = true };
        save.Click += (_, _) => SaveConfig();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.SetColumnSpan(buttons, 2);
        root.Controls.Add(buttons, 0, 3);

        Controls.Add(root);
    }

    private void SaveConfig()
    {
        _grid.EndEdit();

        var name = _nameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Inserisci il nome della configurazione.", "Nuova configurazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var entries = _rows
            .Where(x => !string.IsNullOrWhiteSpace(x.Repository) || !string.IsNullOrWhiteSpace(x.Branch))
            .Select(x => new TaskEntry(x.Repository.Trim(), x.Branch.Trim()))
            .ToList();

        if (entries.Count == 0)
        {
            MessageBox.Show("Inserisci almeno un repository.", "Nuova configurazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (entries.Any(x => string.IsNullOrWhiteSpace(x.Repository) || string.IsNullOrWhiteSpace(x.Branch)))
        {
            MessageBox.Show("Ogni riga deve avere sia repository sia branch.", "Nuova configurazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var duplicate = entries
            .GroupBy(x => x.Repository, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
        {
            MessageBox.Show($"Il repository '{duplicate.Key}' è presente più di una volta.", "Nuova configurazione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            if (_configService.TaskExists(name))
            {
                var overwrite = MessageBox.Show(
                    $"La configurazione '{name}' esiste già. Vuoi sovrascriverla?",
                    "Configurazione esistente",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (overwrite != DialogResult.Yes)
                    return;
            }

            _configService.SaveTask(name, entries);
            CreatedTaskName = name;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed class ConfigRow
    {
        public string Repository { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
    }
}
