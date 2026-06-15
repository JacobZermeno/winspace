using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Winspace.Helpers;
using Winspace.Models;

namespace Winspace.Services;

public class WindowLayoutService
{
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private static (int w, int h) GetScreenSize()
        => (GetSystemMetrics(SM_CXSCREEN), GetSystemMetrics(SM_CYSCREEN));

    /// <summary>
    /// Captures current window positions for all running processes in the profile's launch list.
    /// </summary>
    public WindowLayout CaptureCurrentLayout(WorkspaceProfile profile)
    {
        var pidMap = NativeWindowHelper.GetVisibleWindowsByPid();
        var snapshots = new List<WindowSnapshot>();

        foreach (var entry in profile.LaunchList)
        {
            if (!entry.IsValid) continue;
            var exeName = System.IO.Path.GetFileNameWithoutExtension(entry.Path);
            var processes = Process.GetProcessesByName(exeName);

            foreach (var proc in processes)
            {
                if (!pidMap.TryGetValue((uint)proc.Id, out var hWnd)) continue;
                if (!NativeWindowHelper.GetWindowRect(hWnd, out var rect)) continue;

                snapshots.Add(new WindowSnapshot(
                    exeName,
                    rect.Left, rect.Top,
                    rect.Width, rect.Height,
                    NativeWindowHelper.IsWindowMaximized(hWnd)
                ));
                break;
            }
        }

        return new WindowLayout
        {
            Name = "Custom Layout",
            Preset = LayoutPreset.Custom,
            Snapshots = snapshots
        };
    }

    /// <summary>
    /// Applies a layout to currently running processes.
    /// Waits (up to 8 s) for launch-list apps to show visible windows before positioning.
    /// </summary>
    public async Task ApplyLayoutAsync(WindowLayout layout, WorkspaceProfile profile, IProgress<string>? progress = null)
    {
        // Capture screen dimensions NOW before any await (P/Invoke must run on a thread that
        // has access to the correct desktop DPI context; capture early to be safe).
        var (screenW, screenH) = GetScreenSize();

        var apps = profile.LaunchList.Where(e => e.IsValid).ToList();

        if (apps.Count > 0)
        {
            // Poll every 300 ms until all launch-list apps have visible windows, or 8 s elapses.
            var deadline = DateTime.UtcNow.AddSeconds(8);
            int lastFound = 0;
            int stableFor = 0;

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(300).ConfigureAwait(false);
                var pidMap = NativeWindowHelper.GetVisibleWindowsByPid();
                int found = CountLaunchListWindows(apps, pidMap);

                if (found >= apps.Count)
                    break; // every app has a window — done waiting

                if (found > 0 && found == lastFound)
                {
                    stableFor++;
                    if (stableFor >= 3) break; // count didn't change for ~900 ms — stop waiting
                }
                else
                {
                    stableFor = 0;
                }

                lastFound = found;
                if (found > 0)
                    progress?.Report($"Waiting for apps… {found}/{apps.Count}");
            }

            // Small settle so windows finish their own startup animations
            await Task.Delay(200).ConfigureAwait(false);
        }
        else
        {
            await Task.Delay(500).ConfigureAwait(false);
        }

        if (layout.Preset != LayoutPreset.Custom)
        {
            ApplyPreset(layout.Preset, profile, progress, screenW, screenH);
            return;
        }

        // Custom (captured) layout
        var finalPidMap = NativeWindowHelper.GetVisibleWindowsByPid();
        foreach (var snapshot in layout.Snapshots)
        {
            var processes = Process.GetProcessesByName(snapshot.ProcessName);
            foreach (var proc in processes)
            {
                if (!finalPidMap.TryGetValue((uint)proc.Id, out var hWnd)) continue;
                progress?.Report($"Positioning {snapshot.ProcessName}…");
                NativeWindowHelper.MoveWindow(hWnd, snapshot.X, snapshot.Y,
                    snapshot.Width, snapshot.Height, snapshot.IsMaximized);
                break;
            }
        }
    }

    /// <summary>
    /// Counts how many apps in the launch list currently have a visible window.
    /// </summary>
    private static int CountLaunchListWindows(List<AppEntry> apps, Dictionary<uint, IntPtr> pidMap)
    {
        int count = 0;
        foreach (var app in apps)
        {
            var exeName = System.IO.Path.GetFileNameWithoutExtension(app.Path);
            var procs = Process.GetProcessesByName(exeName);
            if (procs.Any(p => pidMap.ContainsKey((uint)p.Id)))
                count++;
        }
        return count;
    }

    private void ApplyPreset(LayoutPreset preset, WorkspaceProfile profile, IProgress<string>? progress,
        int screenWidth, int screenHeight)
    {
        var apps = profile.LaunchList.Where(e => e.IsValid).ToList();
        var pidMap = NativeWindowHelper.GetVisibleWindowsByPid();

        progress?.Report($"Applying preset: {WindowLayout.PresetDisplayNames[preset]}");

        switch (preset)
        {
            case LayoutPreset.SplitFiftyFifty:
                PositionApp(apps, 0, pidMap, 0, 0, screenWidth / 2, screenHeight);
                PositionApp(apps, 1, pidMap, screenWidth / 2, 0, screenWidth / 2, screenHeight);
                break;

            case LayoutPreset.ThreeColumnGrid:
                var colW = screenWidth / 3;
                PositionApp(apps, 0, pidMap, 0, 0, colW, screenHeight);
                PositionApp(apps, 1, pidMap, colW, 0, colW, screenHeight);
                PositionApp(apps, 2, pidMap, colW * 2, 0, colW, screenHeight);
                break;

            case LayoutPreset.PrimaryCenterFocus:
                var centerW = (int)(screenWidth * 0.6);
                var sideW = (int)(screenWidth * 0.2);
                var centerX = sideW;
                PositionApp(apps, 0, pidMap, centerX, 0, centerW, screenHeight);
                PositionApp(apps, 1, pidMap, 0, 0, sideW, screenHeight);
                PositionApp(apps, 2, pidMap, screenWidth - sideW, 0, sideW, screenHeight);
                break;

            case LayoutPreset.FullScreen:
                PositionApp(apps, 0, pidMap, 0, 0, screenWidth, screenHeight, maximize: true);
                break;
        }
    }

    private static void PositionApp(
        List<AppEntry> apps, int index,
        Dictionary<uint, IntPtr> pidMap,
        int x, int y, int width, int height,
        bool maximize = false)
    {
        if (index >= apps.Count) return;
        var exeName = System.IO.Path.GetFileNameWithoutExtension(apps[index].Path);
        var processes = Process.GetProcessesByName(exeName);
        foreach (var proc in processes)
        {
            if (!pidMap.TryGetValue((uint)proc.Id, out var hWnd)) continue;
            NativeWindowHelper.MoveWindow(hWnd, x, y, width, height, maximize);
            return;
        }
    }
}
