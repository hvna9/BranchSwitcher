namespace BranchSwitcher.Models;

public sealed class AppSettings
{
    public string ConfigsDirectory { get; set; } = "configs";
    public string GitExecutable { get; set; } = "git";
    public Dictionary<string, string> Repositories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
