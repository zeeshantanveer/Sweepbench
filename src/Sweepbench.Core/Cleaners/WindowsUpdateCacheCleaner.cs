using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

/// <summary>
/// SoftwareDistribution\Download — payloads for updates already installed, or
/// staged for an update that hasn't run yet. Windows re-downloads whatever it
/// still needs, so this is always safe to clear.
/// </summary>
public sealed class WindowsUpdateCacheCleaner : ICleaner
{
    public string Id => "windows-update-cache";

    public string DisplayName => "Windows Update Cache";

    public CleanCategory Category => CleanCategory.WindowsUpdateCache;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var root = Path.Combine(windowsDir, "SoftwareDistribution", "Download");

        if (!Directory.Exists(root))
        {
            return Task.FromResult<IReadOnlyList<CleanItem>>([]);
        }

        var items = EntryScanner
            .ScanTopLevel(root, "Windows Update cache", CleanCategory.WindowsUpdateCache, RiskLevel.Safe, "wu-cache")
            .ToList();

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }
}
