using Microsoft.Win32;
using Sweepbench.Core.Models;

namespace Sweepbench.Core.Startup;

public sealed record StartupToggleOutcome(bool Success, string? Error);

/// <summary>
/// Enables or disables a startup item the same way Task Manager's Startup tab does:
/// by writing the enabled/disabled byte into the matching StartupApproved value,
/// never by deleting the Run entry or moving the shortcut. Nothing about the
/// program itself is touched — the toggle is fully reversible either from
/// Sweepbench or from Task Manager directly.
/// </summary>
public sealed class StartupToggleService
{
    public StartupToggleOutcome SetEnabled(StartupItem item, bool enabled)
    {
        try
        {
            var (hive, path, valueName) = ResolveApprovedTarget(item);
            using var key = hive.CreateSubKey(path, writable: true);
            var existing = key.GetValue(valueName) as byte[];
            key.SetValue(valueName, BuildBlob(enabled, existing), RegistryValueKind.Binary);
            return new StartupToggleOutcome(true, null);
        }
        catch (UnauthorizedAccessException)
        {
            return new StartupToggleOutcome(false, "Requires administrator privileges.");
        }
        catch (Exception ex)
        {
            return new StartupToggleOutcome(false, ex.Message);
        }
    }

    private static (RegistryKey Hive, string Path, string ValueName) ResolveApprovedTarget(StartupItem item) => item.Location switch
    {
        StartupLocation.RunKeyCurrentUser => (Microsoft.Win32.Registry.CurrentUser, StartupApprovedPaths.Run, item.Name),
        StartupLocation.RunKeyLocalMachine => (Microsoft.Win32.Registry.LocalMachine, StartupApprovedPaths.Run, item.Name),
        StartupLocation.StartupFolderUser or StartupLocation.StartupFolderCommon =>
            (Microsoft.Win32.Registry.CurrentUser, StartupApprovedPaths.StartupFolder, Path.GetFileName(item.Command)),
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.Location, "Unknown startup location."),
    };

    // Layout is the reverse-engineered format Explorer itself writes: byte 0 is the
    // enabled (0x02) / disabled (0x03) flag, the rest is an opaque timestamp we
    // preserve if present and zero-fill otherwise — Windows doesn't validate it.
    private static byte[] BuildBlob(bool enabled, byte[]? existing)
    {
        var blob = existing is { Length: >= 12 } ? (byte[])existing.Clone() : new byte[12];
        blob[0] = enabled ? (byte)0x02 : (byte)0x03;
        return blob;
    }
}
