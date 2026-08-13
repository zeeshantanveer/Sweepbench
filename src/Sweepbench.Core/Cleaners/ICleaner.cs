using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

/// <summary>
/// A single scan source (temp files, a browser's cache, the Recycle Bin, ...).
/// Scanning only ever reads and measures — nothing is deleted until a
/// <see cref="CleanItem"/> it returned is passed to the executor.
/// </summary>
public interface ICleaner
{
    string Id { get; }

    string DisplayName { get; }

    CleanCategory Category { get; }

    Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default);
}
