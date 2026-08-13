using Sweepbench.Core.Models;
using Sweepbench.Core.Registry;

namespace Sweepbench.Core.Cleaners;

/// <summary>
/// "App Paths" maps a bare exe name (what <c>Start-Process notepad.exe</c>-style
/// lookups use) to a full path. An entry whose target no longer exists is dead
/// weight left behind by an uninstaller that didn't clean up after itself — removing
/// it can't break anything still installed, but it's still a registry edit, so it
/// stays <see cref="RiskLevel.Caution"/> (unchecked by default) rather than
/// <see cref="RiskLevel.Safe"/>.
/// </summary>
public sealed class RegistryOrphanedAppPathsCleaner : ICleaner
{
    private const string SubKeyRoot = @"Software\Microsoft\Windows\CurrentVersion\App Paths";

    public string Id => "registry-orphaned-app-paths";

    public string DisplayName => "Orphaned App Paths";

    public CleanCategory Category => CleanCategory.Registry;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanItem>();

        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.AddRange(ScanHive(hive));
        }

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }

    private static IEnumerable<CleanItem> ScanHive(RegistryHive hive)
    {
        var baseKey = hive.ToBaseKey();
        using var root = baseKey.OpenSubKey(SubKeyRoot, writable: false);
        if (root is null)
        {
            yield break;
        }

        foreach (var exeName in root.GetSubKeyNames())
        {
            using var subKey = root.OpenSubKey(exeName, writable: false);
            var targetPath = subKey?.GetValue(null) as string;

            if (!RegistryPathHeuristics.LooksLikeExecutablePath(targetPath))
            {
                continue; // Not a plain absolute .exe path — don't guess, skip it.
            }

            if (File.Exists(targetPath))
            {
                continue;
            }

            var subKeyPath = $@"{SubKeyRoot}\{exeName}";
            yield return new CleanItem(
                Id: $"registry-app-paths:{hive}:{subKeyPath}",
                DisplayName: exeName,
                Description: $@"App Paths entry points to missing file — {targetPath}",
                Path: $@"{HiveLabel(hive)}\{subKeyPath}",
                SizeBytes: 1,
                Category: CleanCategory.Registry,
                Risk: RiskLevel.Caution,
                Kind: CleanItemKind.RegistryValue,
                Registry: new RegistryTarget(hive, subKeyPath, ValueName: null));
        }
    }

    private static string HiveLabel(RegistryHive hive) => hive == RegistryHive.CurrentUser ? "HKCU" : "HKLM";
}
