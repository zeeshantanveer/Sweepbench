using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

/// <summary>
/// Shared logic for cleaners that turn "top-level entries of a directory" into
/// individual <see cref="CleanItem"/>s (temp folders, the Windows Update download
/// cache). Each entry becomes one row so a partial delete of a locked sibling
/// doesn't block the rest.
/// </summary>
internal static class EntryScanner
{
    public static IEnumerable<CleanItem> ScanTopLevel(
        string root,
        string label,
        CleanCategory category,
        RiskLevel risk,
        string idPrefix)
    {
        string[] entries;
        try
        {
            entries = Directory.GetFileSystemEntries(root);
        }
        catch
        {
            yield break;
        }

        foreach (var entry in entries)
        {
            var (kind, size) = Measure(entry);
            if (size <= 0)
            {
                continue;
            }

            yield return new CleanItem(
                Id: $"{idPrefix}:{entry}",
                DisplayName: Path.GetFileName(entry),
                Description: $"{label} — {entry}",
                Path: entry,
                SizeBytes: size,
                Category: category,
                Risk: risk,
                Kind: kind);
        }
    }

    private static (CleanItemKind Kind, long Size) Measure(string entry)
    {
        try
        {
            if (Directory.Exists(entry))
            {
                return (CleanItemKind.Directory, DirectorySize.Calculate(entry));
            }

            return (CleanItemKind.File, new FileInfo(entry).Length);
        }
        catch
        {
            return (CleanItemKind.File, 0);
        }
    }
}
