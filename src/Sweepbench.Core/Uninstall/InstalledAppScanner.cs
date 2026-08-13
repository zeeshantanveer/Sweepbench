using Sweepbench.Core.Models;
using Sweepbench.Core.Registry;

namespace Sweepbench.Core.Uninstall;

/// <summary>
/// Enumerates the same three Uninstall registry roots Windows' own "Apps &amp;
/// features" reads. Unlike <see cref="Cleaners.RegistryOrphanedUninstallCleaner"/>
/// this keeps every entry with a working uninstall command — it's a launcher, not
/// an orphan detector.
/// </summary>
public sealed class InstalledAppScanner
{
    private static readonly (RegistryHive Hive, string SubKeyPath)[] Roots =
    [
        (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryHive.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
    ];

    public Task<IReadOnlyList<InstalledApp>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<InstalledApp>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (hive, rootPath) in Roots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var root = hive.ToBaseKey().OpenSubKey(rootPath, writable: false);
            if (root is null)
            {
                continue;
            }

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                var app = TryRead(hive, rootPath, subKeyName);
                if (app is not null && seen.Add(app.Id))
                {
                    items.Add(app);
                }
            }
        }

        var sorted = items.OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        return Task.FromResult<IReadOnlyList<InstalledApp>>(sorted);
    }

    private static InstalledApp? TryRead(RegistryHive hive, string rootPath, string subKeyName)
    {
        var subKeyPath = $@"{rootPath}\{subKeyName}";
        using var key = hive.ToBaseKey().OpenSubKey(subKeyPath, writable: false);
        if (key is null)
        {
            return null;
        }

        var displayName = key.GetValue("DisplayName") as string;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        if (key.GetValue("SystemComponent") is int systemComponent && systemComponent == 1)
        {
            return null;
        }

        if (key.GetValue("ParentKeyName") is string { Length: > 0 })
        {
            return null; // A child/update entry, not a standalone app.
        }

        var uninstallString = key.GetValue("UninstallString") as string;
        if (string.IsNullOrWhiteSpace(uninstallString))
        {
            return null;
        }

        var publisher = key.GetValue("Publisher") as string;
        var version = key.GetValue("DisplayVersion") as string;
        var sizeKb = key.GetValue("EstimatedSize") is int kb ? kb : 0;

        return new InstalledApp(
            Id: $"installed-app:{hive}:{subKeyPath}",
            DisplayName: displayName,
            Publisher: publisher,
            Version: version,
            EstimatedSizeBytes: (long)sizeKb * 1024,
            UninstallCommand: uninstallString);
    }
}
