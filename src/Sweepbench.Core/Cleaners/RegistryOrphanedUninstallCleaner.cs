using Sweepbench.Core.Models;
using Sweepbench.Core.Registry;

namespace Sweepbench.Core.Cleaners;

/// <summary>
/// Add/Remove Programs entries whose uninstaller no longer exists on disk — the app
/// was removed by hand or its files deleted directly, leaving a dead entry behind.
/// MSI-managed entries (<c>WindowsInstaller=1</c>, or an <c>MsiExec.exe /X{GUID}</c>
/// command) are skipped entirely: there's no file path to check, and a GUID lookup
/// against the MSI database is out of scope for an orphan *heuristic*.
/// </summary>
public sealed class RegistryOrphanedUninstallCleaner : ICleaner
{
    private static readonly (RegistryHive Hive, string SubKeyPath)[] Roots =
    [
        (RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryHive.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
        (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
    ];

    public string Id => "registry-orphaned-uninstall";

    public string DisplayName => "Orphaned Uninstall Entries";

    public CleanCategory Category => CleanCategory.Registry;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanItem>();
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
                var item = TryEvaluate(hive, rootPath, subKeyName);
                if (item is not null && seen.Add(item.Id))
                {
                    items.Add(item);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }

    private static CleanItem? TryEvaluate(RegistryHive hive, string rootPath, string subKeyName)
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

        if (key.GetValue("WindowsInstaller") is int windowsInstaller && windowsInstaller == 1)
        {
            return null; // MSI-managed — no file path we can safely check.
        }

        var uninstallString = key.GetValue("UninstallString") as string;
        var exePath = RegistryPathHeuristics.ExtractLeadingExecutablePath(uninstallString);
        if (exePath is null)
        {
            return null; // Couldn't confidently extract a checkable path — don't guess.
        }

        if (File.Exists(exePath))
        {
            return null;
        }

        return new CleanItem(
            Id: $"registry-uninstall:{hive}:{subKeyPath}",
            DisplayName: displayName,
            Description: $"Uninstaller missing — {exePath}",
            Path: $@"{(hive == RegistryHive.CurrentUser ? "HKCU" : "HKLM")}\{subKeyPath}",
            SizeBytes: 1,
            Category: CleanCategory.Registry,
            Risk: RiskLevel.Caution,
            Kind: CleanItemKind.RegistryValue,
            Registry: new RegistryTarget(hive, subKeyPath, ValueName: null));
    }
}
