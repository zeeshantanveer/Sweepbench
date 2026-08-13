using System.Diagnostics;
using Sweepbench.Core.Cleaners;
using Sweepbench.Core.Models;

namespace Sweepbench.Core.Engine;

/// <summary>
/// Runs every registered <see cref="ICleaner"/> and merges the results into one
/// <see cref="ScanResult"/>. Nothing here ever deletes anything — scanning is
/// read-only by construction; only <see cref="CleanExecutor"/> touches disk.
/// </summary>
public sealed class ScanEngine
{
    private readonly IReadOnlyList<ICleaner> _cleaners;

    public ScanEngine(IEnumerable<ICleaner> cleaners)
    {
        _cleaners = cleaners.ToList();
    }

    public static ScanEngine CreateDefault() => new(
    [
        new TempFilesCleaner(),
        new BrowserCacheCleaner(),
        new RecycleBinCleaner(),
        new WindowsUpdateCacheCleaner(),
    ]);

    public static ScanEngine CreateRegistryScan() => new(
    [
        new RegistryMruCleaner(),
        new RegistryOrphanedAppPathsCleaner(),
        new RegistryOrphanedUninstallCleaner(),
    ]);

    public async Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var perCleanerResults = await Task.WhenAll(
            _cleaners.Select(cleaner => ScanOneAsync(cleaner, cancellationToken)));

        stopwatch.Stop();

        var items = perCleanerResults.SelectMany(r => r).ToList();
        return new ScanResult(items, DateTimeOffset.Now, stopwatch.Elapsed);
    }

    // One misbehaving cleaner (e.g. a folder ACL that throws instead of denying
    // enumeration) should never take the rest of the scan down with it.
    private static async Task<IReadOnlyList<CleanItem>> ScanOneAsync(ICleaner cleaner, CancellationToken cancellationToken)
    {
        try
        {
            return await cleaner.ScanAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return [];
        }
    }
}
