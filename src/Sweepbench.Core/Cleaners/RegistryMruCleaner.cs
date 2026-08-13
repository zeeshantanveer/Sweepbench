using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

/// <summary>
/// Clears "most recently used" history lists. These are pure history — Explorer
/// recreates each key from scratch the next time it needs one — so every item here
/// is <see cref="RiskLevel.Safe"/>. RecentDocs is deliberately excluded: its useful
/// data lives in nested per-extension subkeys our backup writer doesn't walk into,
/// and clearing it without a matching backup would be a hollow safety promise.
/// </summary>
public sealed class RegistryMruCleaner : ICleaner
{
    private static readonly (string SubKeyPath, string Label)[] Targets =
    [
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU", "Run dialog history"),
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths", "Explorer address bar history"),
        (@"Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery", "Explorer search history"),
    ];

    public string Id => "registry-mru";

    public string DisplayName => "Recently Used History";

    public CleanCategory Category => CleanCategory.Registry;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CleanItem>();

        foreach (var (subKeyPath, label) in Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKeyPath, writable: false);
            var valueCount = key?.GetValueNames().Length ?? 0;
            if (valueCount == 0)
            {
                continue;
            }

            items.Add(new CleanItem(
                Id: $"registry-mru:{subKeyPath}",
                DisplayName: label,
                Description: $@"HKCU\{subKeyPath} — {valueCount} entr{(valueCount == 1 ? "y" : "ies")}",
                Path: $@"HKCU\{subKeyPath}",
                SizeBytes: valueCount,
                Category: CleanCategory.Registry,
                Risk: RiskLevel.Safe,
                Kind: CleanItemKind.RegistryValue,
                Registry: new RegistryTarget(RegistryHive.CurrentUser, subKeyPath, ValueName: null)));
        }

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }
}
