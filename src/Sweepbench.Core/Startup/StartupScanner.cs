using Sweepbench.Core.Models;

namespace Sweepbench.Core.Startup;

/// <summary>
/// Enumerates the same four sources Windows' own Task Manager Startup tab reads —
/// HKCU/HKLM Run keys and the two Startup folders — and cross-references the
/// StartupApproved keys Windows itself uses to record enabled/disabled state, so
/// what Sweepbench shows matches what Task Manager would show.
/// </summary>
public sealed class StartupScanner
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public Task<IReadOnlyList<StartupItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<StartupItem>();

        items.AddRange(ScanRunKey(Microsoft.Win32.Registry.CurrentUser, StartupLocation.RunKeyCurrentUser));
        cancellationToken.ThrowIfCancellationRequested();

        items.AddRange(ScanRunKey(Microsoft.Win32.Registry.LocalMachine, StartupLocation.RunKeyLocalMachine));
        cancellationToken.ThrowIfCancellationRequested();

        items.AddRange(ScanFolder(Environment.SpecialFolder.Startup, StartupLocation.StartupFolderUser));
        cancellationToken.ThrowIfCancellationRequested();

        items.AddRange(ScanFolder(Environment.SpecialFolder.CommonStartup, StartupLocation.StartupFolderCommon));

        return Task.FromResult<IReadOnlyList<StartupItem>>(items);
    }

    private static IEnumerable<StartupItem> ScanRunKey(Microsoft.Win32.RegistryKey hive, StartupLocation location)
    {
        using var runKey = hive.OpenSubKey(RunKeyPath, writable: false);
        if (runKey is null)
        {
            yield break;
        }

        using var approvedKey = hive.OpenSubKey(StartupApprovedPaths.Run, writable: false);

        foreach (var valueName in runKey.GetValueNames())
        {
            if (string.IsNullOrEmpty(valueName))
            {
                continue;
            }

            var command = runKey.GetValue(valueName) as string ?? string.Empty;

            yield return new StartupItem(
                Id: $"startup-run:{location}:{valueName}",
                Name: valueName,
                Command: command,
                Location: location,
                IsEnabled: IsApprovedEnabled(approvedKey, valueName));
        }
    }

    private static IEnumerable<StartupItem> ScanFolder(Environment.SpecialFolder folder, StartupLocation location)
    {
        var path = Environment.GetFolderPath(folder);
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            yield break;
        }

        using var approvedKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(StartupApprovedPaths.StartupFolder, writable: false);

        string[] files;
        try
        {
            files = Directory.GetFiles(path);
        }
        catch
        {
            yield break;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new StartupItem(
                Id: $"startup-folder:{location}:{fileName}",
                Name: Path.GetFileNameWithoutExtension(fileName),
                Command: file,
                Location: location,
                IsEnabled: IsApprovedEnabled(approvedKey, fileName));
        }
    }

    // Absence of a StartupApproved entry means Windows has never recorded a
    // preference for this item — its default state is enabled.
    private static bool IsApprovedEnabled(Microsoft.Win32.RegistryKey? approvedKey, string valueName)
    {
        if (approvedKey?.GetValue(valueName) is not byte[] { Length: > 0 } blob)
        {
            return true;
        }

        return blob[0] == 0x02;
    }
}
