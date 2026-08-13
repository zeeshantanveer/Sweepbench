using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

public sealed class TempFilesCleaner : ICleaner
{
    public string Id => "temp-files";

    public string DisplayName => "Temporary Files";

    public CleanCategory Category => CleanCategory.TempFiles;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var roots = new (string Path, string Label)[]
        {
            (Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(), "User temp"),
            (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"), "Windows temp"),
        };

        var items = new List<CleanItem>();
        foreach (var (root, label) in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(root))
            {
                continue;
            }

            items.AddRange(EntryScanner.ScanTopLevel(root, label, CleanCategory.TempFiles, RiskLevel.Safe, "temp"));
        }

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }
}
