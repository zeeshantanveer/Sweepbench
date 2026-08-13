namespace Sweepbench.Core.Models;

public enum StartupLocation
{
    RunKeyCurrentUser,
    RunKeyLocalMachine,
    StartupFolderUser,
    StartupFolderCommon,
}

/// <summary>One program Windows launches at sign-in, and whether it currently will.</summary>
public sealed record StartupItem(
    string Id,
    string Name,
    string Command,
    StartupLocation Location,
    bool IsEnabled);
