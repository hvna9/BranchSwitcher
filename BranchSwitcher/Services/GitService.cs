using System.Diagnostics;
using BranchSwitcher.Models;

namespace BranchSwitcher.Services;

public sealed class GitService
{
    public async Task<string> GetCurrentBranchAsync(string gitExe, string repoPath, CancellationToken token = default)
    {
        var result = await RunGitAsync(gitExe, repoPath, token, "branch", "--show-current");
        return result.ExitCode == 0 ? result.StdOut.Trim() : "(errore)";
    }

    public async Task<GitOperationResult> ApplyAsync(
        string gitExe,
        string repoAlias,
        string repoPath,
        string targetBranch,
        Action<string>? log = null,
        CancellationToken token = default)
    {
        if (!Directory.Exists(repoPath))
            return new(false, repoAlias, targetBranch, $"Directory non trovata: {repoPath}");

        var inside = await RunGitAsync(gitExe, repoPath, token, "rev-parse", "--is-inside-work-tree");
        if (inside.ExitCode != 0 || !inside.StdOut.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            return new(false, repoAlias, targetBranch, "La directory non risulta essere un repository Git.");

        var status = await RunGitAsync(gitExe, repoPath, token, "status", "--porcelain");
        if (status.ExitCode != 0)
            return new(false, repoAlias, targetBranch, "Impossibile leggere lo stato Git: " + status.StdErr.Trim());

        if (!string.IsNullOrWhiteSpace(status.StdOut))
            return new(false, repoAlias, targetBranch, "Working tree non pulito. Commit/stash/rimuovi le modifiche prima dello switch.\r\n" + status.StdOut.TrimEnd());

        log?.Invoke($"[{repoAlias}] git fetch --prune");
        var fetch = await RunGitAsync(gitExe, repoPath, token, "fetch", "--prune");
        if (fetch.ExitCode != 0)
            return new(false, repoAlias, targetBranch, "git fetch fallito: " + Combined(fetch));

        var current = await GetCurrentBranchAsync(gitExe, repoPath, token);
        log?.Invoke($"[{repoAlias}] branch corrente: {current}");

        if (!current.Equals(targetBranch, StringComparison.Ordinal))
        {
            var localExists = await RunGitAsync(gitExe, repoPath, token, "show-ref", "--verify", "--quiet", $"refs/heads/{targetBranch}");

            if (localExists.ExitCode == 0)
            {
                log?.Invoke($"[{repoAlias}] git switch {targetBranch}");
                var sw = await RunGitAsync(gitExe, repoPath, token, "switch", targetBranch);
                if (sw.ExitCode != 0)
                    return new(false, repoAlias, targetBranch, "git switch fallito: " + Combined(sw));
            }
            else
            {
                var remoteExists = await RunGitAsync(gitExe, repoPath, token, "show-ref", "--verify", "--quiet", $"refs/remotes/origin/{targetBranch}");
                if (remoteExists.ExitCode != 0)
                    return new(false, repoAlias, targetBranch, $"Branch '{targetBranch}' non trovato localmente né su origin.");

                log?.Invoke($"[{repoAlias}] creazione branch locale da origin/{targetBranch}");
                var track = await RunGitAsync(gitExe, repoPath, token, "switch", "--track", $"origin/{targetBranch}");
                if (track.ExitCode != 0)
                    return new(false, repoAlias, targetBranch, "Creazione branch locale fallita: " + Combined(track));
            }
        }
        else
        {
            log?.Invoke($"[{repoAlias}] già sul branch richiesto");
        }

        log?.Invoke($"[{repoAlias}] git pull --ff-only");
        var pull = await RunGitAsync(gitExe, repoPath, token, "pull", "--ff-only");
        if (pull.ExitCode != 0)
            return new(false, repoAlias, targetBranch, "git pull --ff-only fallito. Possibile divergenza locale/remota. " + Combined(pull));

        return new(true, repoAlias, targetBranch, $"Aggiornato su {targetBranch}. {pull.StdOut.Trim()}");
    }

    private static string Combined(ProcessResult result)
    {
        return string.Join(" ", new[] { result.StdOut.Trim(), result.StdErr.Trim() }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static async Task<ProcessResult> RunGitAsync(string gitExe, string workingDirectory, CancellationToken token, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = gitExe,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(token);
        var stderrTask = process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token);

        return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);
}
