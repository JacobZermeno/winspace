using System.Diagnostics;
using Winspace.Helpers;
using Winspace.Models;

namespace Winspace.Services;

public class ProcessService
{
    // Processes that must never be touched under any circumstances
    private static readonly HashSet<string> HardcodedWhitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "audiodg", "winlogon", "csrss", "lsass", "services",
        "svchost", "dwm", "taskmgr", "SearchHost", "ShellExperienceHost",
        "StartMenuExperienceHost", "RuntimeBroker", "sihost", "fontdrvhost",
        "winspace" // never kill ourselves
    };

    private readonly ConfigService _config;

    public ProcessService(ConfigService config)
    {
        _config = config;
    }

    private HashSet<string> BuildFullWhitelist()
    {
        var list = new HashSet<string>(HardcodedWhitelist, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in _config.Settings.UserWhitelist)
            list.Add(entry);
        return list;
    }

    /// <summary>Launches all apps in the profile's launch list.</summary>
    public async Task LaunchAppsAsync(WorkspaceProfile profile, IProgress<string>? progress = null)
    {
        foreach (var entry in profile.LaunchList)
        {
            if (!entry.IsValid) continue;
            try
            {
                progress?.Report($"Launching {entry.DisplayName}…");
                var psi = new ProcessStartInfo
                {
                    FileName = entry.Path,
                    Arguments = entry.Arguments,
                    UseShellExecute = true
                };
                Process.Start(psi);
                await Task.Delay(300).ConfigureAwait(false); // brief stagger
            }
            catch (Exception ex)
            {
                progress?.Report($"Failed to launch {entry.DisplayName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Closes all processes on the kill list.
    /// Attempts graceful WM_CLOSE first, then force-kills after timeout.
    /// </summary>
    public async Task KillAppsAsync(WorkspaceProfile profile, IProgress<string>? progress = null)
    {
        var whitelist = BuildFullWhitelist();
        var timeoutMs = _config.Settings.GracefulCloseTimeoutMs;

        foreach (var name in profile.KillList)
        {
            if (whitelist.Contains(name))
            {
                progress?.Report($"Skipping whitelisted: {name}");
                continue;
            }

            var processes = Process.GetProcessesByName(name);
            foreach (var proc in processes)
            {
                await CloseProcessAsync(proc, timeoutMs, progress);
            }
        }
    }

    private async Task CloseProcessAsync(Process proc, int timeoutMs, IProgress<string>? progress)
    {
        try
        {
            progress?.Report($"Closing {proc.ProcessName}…");

            // Step 1: find the main window handle and post WM_CLOSE
            var hwnd = proc.MainWindowHandle;
            if (hwnd != IntPtr.Zero)
            {
                NativeWindowHelper.PostMessage(hwnd, NativeWindowHelper.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

                // Wait up to timeout for graceful exit
                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (!proc.HasExited && DateTime.UtcNow < deadline)
                    await Task.Delay(200).ConfigureAwait(false);
            }

            // Step 2: force-kill if still running
            if (!proc.HasExited)
            {
                progress?.Report($"Force-killing {proc.ProcessName}…");
                proc.Kill();
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"Error closing {proc.ProcessName}: {ex.Message}");
        }
        finally
        {
            proc.Dispose();
        }
    }

    /// <summary>Returns all running process names (excluding system whitelist).</summary>
    public List<string> GetKillableProcessNames()
    {
        var whitelist = BuildFullWhitelist();
        return Process.GetProcesses()
            .Select(p => p.ProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !whitelist.Contains(n))
            .OrderBy(n => n)
            .ToList();
    }

    public static IReadOnlySet<string> GetHardcodedWhitelist() => HardcodedWhitelist;
}
