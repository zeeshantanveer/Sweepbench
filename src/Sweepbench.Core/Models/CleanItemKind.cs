namespace Sweepbench.Core.Models;

/// <summary>What kind of action deleting a <see cref="CleanItem"/> actually performs.</summary>
public enum CleanItemKind
{
    File,
    Directory,

    /// <summary>Not a filesystem path — executing it empties the Recycle Bin.</summary>
    RecycleBinEmpty,
}
