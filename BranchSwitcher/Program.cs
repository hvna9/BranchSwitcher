using BranchSwitcher.Services;

namespace BranchSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var appDirectory = AppContext.BaseDirectory;
        var settingsService = new SettingsService(appDirectory);
        var configService = new ConfigService(appDirectory, settingsService);
        var gitService = new GitService();

        Application.Run(new MainForm(settingsService, configService, gitService));
    }
}
