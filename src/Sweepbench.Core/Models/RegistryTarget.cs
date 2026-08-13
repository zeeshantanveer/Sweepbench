namespace Sweepbench.Core.Models;

public enum RegistryHive
{
    CurrentUser,
    LocalMachine,
}

/// <summary>
/// Points at either a single value or a whole subkey. When <see cref="ValueName"/> is
/// null the executor deletes the subkey itself (used for MRU lists, where the "item"
/// is the whole history key, not one entry in it).
/// </summary>
public sealed record RegistryTarget(RegistryHive Hive, string SubKeyPath, string? ValueName);
