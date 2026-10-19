using System.Text;
using BranchSwitcher.Models;

namespace BranchSwitcher.Services;

public sealed class ConfigService
{
    private readonly string _appDirectory;
    private readonly SettingsService _settingsService;

    public ConfigService(string appDirectory, SettingsService settingsService)
    {
        _appDirectory = appDirectory;
        _settingsService = settingsService;
    }

    public string GetConfigsDirectory()
    {
        var settings = _settingsService.Load();
        return Path.GetFullPath(Path.Combine(_appDirectory, settings.ConfigsDirectory));
    }

    public IReadOnlyList<string> GetAvailableTasks()
    {
        var dir = GetConfigsDirectory();
        Directory.CreateDirectory(dir);

        return Directory.GetFiles(dir, "*.conf", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<TaskEntry> LoadTask(string taskName)
    {
        var path = GetTaskPath(taskName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Configurazione non trovata.", path);

        var result = new List<TaskEntry>();
        var lineNumber = 0;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            lineNumber++;
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1)
                throw new FormatException($"Riga non valida in {Path.GetFileName(path)} alla riga {lineNumber}: {rawLine}");

            var repo = line[..separator].Trim();
            var branch = line[(separator + 1)..].Trim();
            result.Add(new TaskEntry(repo, branch));
        }

        return result;
    }

    public bool TaskExists(string taskName) => File.Exists(GetTaskPath(taskName));

    public void SaveTask(string taskName, IEnumerable<TaskEntry> entries)
    {
        var normalizedName = ValidateTaskName(taskName);
        var dir = GetConfigsDirectory();
        Directory.CreateDirectory(dir);

        var lines = new List<string>
        {
            "# ============================================================",
            $"# {normalizedName}",
            "# ============================================================",
            string.Empty
        };

        lines.AddRange(entries.Select(x => $"{x.Repository}={x.Branch}"));
        lines.Add(string.Empty);

        File.WriteAllLines(Path.Combine(dir, normalizedName + ".conf"), lines, new UTF8Encoding(false));
    }

    public void DeleteTask(string taskName)
    {
        var path = GetTaskPath(taskName);
        if (!File.Exists(path))
            throw new FileNotFoundException("Configurazione non trovata.", path);

        File.Delete(path);
    }

    private string GetTaskPath(string taskName)
    {
        var normalizedName = ValidateTaskName(taskName);
        return Path.Combine(GetConfigsDirectory(), normalizedName + ".conf");
    }

    private static string ValidateTaskName(string taskName)
    {
        var name = taskName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Il nome della configurazione non può essere vuoto.");

        if (name.EndsWith(".conf", StringComparison.OrdinalIgnoreCase))
            name = name[..^5].Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Il nome della configurazione non può essere vuoto.");

        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Contains(Path.DirectorySeparatorChar) || name.Contains(Path.AltDirectorySeparatorChar))
            throw new ArgumentException("Il nome della configurazione contiene caratteri non validi per un file Windows.");

        return name;
    }
}
