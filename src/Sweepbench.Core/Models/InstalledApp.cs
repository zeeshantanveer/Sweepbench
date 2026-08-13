namespace Sweepbench.Core.Models;

public sealed record InstalledApp(
    string Id,
    string DisplayName,
    string? Publisher,
    string? Version,
    long EstimatedSizeBytes,
    string UninstallCommand);
