using Sweepbench.Core.Interop;
using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

public sealed class RecycleBinCleaner : ICleaner
{
    public string Id => "recycle-bin";

    public string DisplayName => "Recycle Bin";

    public CleanCategory Category => CleanCategory.RecycleBin;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var (sizeBytes, itemCount) = RecycleBinInterop.Query();
        var items = new List<CleanItem>();

        if (sizeBytes > 0)
        {
            items.Add(new CleanItem(
                Id: "recycle-bin:empty",
                DisplayName: "Recycle Bin",
                Description: $"{itemCount} item(s) across all drives",
                Path: string.Empty,
                SizeBytes: sizeBytes,
                Category: CleanCategory.RecycleBin,
                Risk: RiskLevel.Safe,
                Kind: CleanItemKind.RecycleBinEmpty));
        }

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }
}
