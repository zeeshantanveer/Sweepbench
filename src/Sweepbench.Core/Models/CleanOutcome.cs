namespace Sweepbench.Core.Models;

public sealed record CleanOutcome(CleanItem Item, bool Success, long BytesFreed, string? Error);

public sealed record CleanProgress(int Completed, int Total, CleanItem CurrentItem);
