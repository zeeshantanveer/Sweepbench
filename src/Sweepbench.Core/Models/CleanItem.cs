namespace Sweepbench.Core.Models;

/// <summary>
/// One reclaimable thing found during a scan. Immutable and UI-agnostic — selection
/// state lives in the caller (the WPF view model), not here.
/// </summary>
public sealed record CleanItem(
    string Id,
    string DisplayName,
    string Description,
    string Path,
    long SizeBytes,
    CleanCategory Category,
    RiskLevel Risk,
    CleanItemKind Kind,
    RegistryTarget? Registry = null);
