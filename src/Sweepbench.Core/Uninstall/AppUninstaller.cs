using System.Diagnostics;
using Sweepbench.Core.Models;

namespace Sweepbench.Core.Uninstall;

public sealed record UninstallOutcome(bool Success, string? Error);

/// <summary>
/// Launches the vendor's own uninstaller and stops there — Sweepbench doesn't try to
/// sweep up "leftover" files or registry keys afterward. That kind of automated
/// cleanup risks deleting something a still-installed app depends on; the honest
/// scope here is "hand off to the uninstaller the vendor shipped," the same thing
/// Windows' own "Apps & features" does.
/// </summary>
public sealed class AppUninstaller
{
    public UninstallOutcome Launch(InstalledApp app)
    {
        try
        {
            // Routed through cmd.exe so quoted paths, arguments, and msiexec-style
            // commands all parse the same way Windows itself would run them —
            // UseShellExecute lets a UAC prompt surface if the uninstaller needs it.
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{app.UninstallCommand}\"",
                UseShellExecute = true,
            };

            Process.Start(startInfo);
            return new UninstallOutcome(true, null);
        }
        catch (Exception ex)
        {
            return new UninstallOutcome(false, ex.Message);
        }
    }
}
