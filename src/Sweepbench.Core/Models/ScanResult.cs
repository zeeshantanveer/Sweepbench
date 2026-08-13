namespace Sweepbench.Core.Models;

public sealed class ScanResult
{
    public ScanResult(IReadOnlyList<CleanItem> items, DateTimeOffset scannedAt, TimeSpan duration)
    {
        Items = items;
        ScannedAt = scannedAt;
        Duration = duration;
    }

    public IReadOnlyList<CleanItem> Items { get; }

    public long TotalBytes => Items.Sum(i => i.SizeBytes);

    public DateTimeOffset ScannedAt { get; }

    public TimeSpan Duration { get; }
}
