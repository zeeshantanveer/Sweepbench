using Microsoft.VisualBasic.FileIO;
using Sweepbench.Core.Interop;
using Sweepbench.Core.Models;
using Sweepbench.Core.Registry;

namespace Sweepbench.Core.Engine;

/// <summary>
/// Executes an explicit, caller-approved selection of <see cref="CleanItem"/>s.
/// Files and folders are sent to the Recycle Bin rather than deleted outright —
/// "clean" stays a reversible action right up until the user empties it themselves.
/// </summary>
public sealed class CleanExecutor
{
    public Task<IReadOnlyList<CleanOutcome>> ExecuteAsync(
        IEnumerable<CleanItem> items,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var list = items.ToList();
        var outcomes = new List<CleanOutcome>(list.Count);

        for (var i = 0; i < list.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = list[i];
            var outcome = CleanOne(item);
            outcomes.Add(outcome);
            progress?.Report(new CleanProgress(i + 1, list.Count, item));
        }

        return Task.FromResult<IReadOnlyList<CleanOutcome>>(outcomes);
    }

    private static CleanOutcome CleanOne(CleanItem item)
    {
        try
        {
            return item.Kind switch
            {
                CleanItemKind.File => CleanFile(item),
                CleanItemKind.Directory => CleanDirectory(item),
                CleanItemKind.RecycleBinEmpty => CleanRecycleBin(item),
                CleanItemKind.RegistryValue => CleanRegistryValue(item),
                _ => new CleanOutcome(item, false, 0, $"Unknown item kind: {item.Kind}"),
            };
        }
        catch (Exception ex)
        {
            return new CleanOutcome(item, false, 0, ex.Message);
        }
    }

    private static CleanOutcome CleanFile(CleanItem item)
    {
        if (!File.Exists(item.Path))
        {
            return new CleanOutcome(item, true, 0, null);
        }

        var size = new FileInfo(item.Path).Length;
        FileSystem.DeleteFile(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        return new CleanOutcome(item, true, size, null);
    }

    private static CleanOutcome CleanDirectory(CleanItem item)
    {
        if (!Directory.Exists(item.Path))
        {
            return new CleanOutcome(item, true, 0, null);
        }

        var size = Cleaners.DirectorySize.Calculate(item.Path);
        FileSystem.DeleteDirectory(item.Path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        return new CleanOutcome(item, true, size, null);
    }

    private static CleanOutcome CleanRecycleBin(CleanItem item)
    {
        RecycleBinInterop.Empty();
        return new CleanOutcome(item, true, item.SizeBytes, null);
    }

    private static CleanOutcome CleanRegistryValue(CleanItem item)
    {
        var target = item.Registry;
        if (target is null)
        {
            return new CleanOutcome(item, false, 0, "Registry item is missing its target.");
        }

        // Capture the value/subkey before touching it — this is Sweepbench's undo log
        // for registry edits (see RegistryBackupWriter for why this stands in for a
        // full System Restore point in Phase 2).
        new RegistryBackupWriter().BackupBeforeDelete(target);

        var baseKey = target.Hive.ToBaseKey();

        if (target.ValueName is not null)
        {
            using var key = baseKey.OpenSubKey(target.SubKeyPath, writable: true);
            key?.DeleteValue(target.ValueName, throwOnMissingValue: false);
            return new CleanOutcome(item, true, item.SizeBytes, null);
        }

        var lastSlash = target.SubKeyPath.LastIndexOf('\\');
        if (lastSlash < 0)
        {
            return new CleanOutcome(item, false, 0, "Refusing to delete a registry hive root.");
        }

        var parentPath = target.SubKeyPath[..lastSlash];
        var leafName = target.SubKeyPath[(lastSlash + 1)..];

        using var parentKey = baseKey.OpenSubKey(parentPath, writable: true);
        parentKey?.DeleteSubKeyTree(leafName, throwOnMissingSubKey: false);
        return new CleanOutcome(item, true, item.SizeBytes, null);
    }
}
