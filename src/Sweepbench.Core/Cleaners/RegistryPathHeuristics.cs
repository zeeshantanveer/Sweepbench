namespace Sweepbench.Core.Cleaners;

/// <summary>
/// Deliberately conservative "is this a real, checkable file path" test shared by the
/// registry orphan cleaners. Anything ambiguous (relative paths, bare command names,
/// MSI product codes) is treated as "can't verify" rather than guessed at — a missed
/// orphan is harmless; a false positive erodes trust in the whole cleaner.
/// </summary>
internal static class RegistryPathHeuristics
{
    public static bool LooksLikeExecutablePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var trimmed = path.Trim().Trim('"');

        if (!trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Absolute drive path (C:\...) or UNC (\\server\share\...) only.
        var isDriveAbsolute = trimmed.Length > 2 && trimmed[1] == ':' && (trimmed[2] == '\\' || trimmed[2] == '/');
        var isUnc = trimmed.StartsWith(@"\\", StringComparison.Ordinal);

        return isDriveAbsolute || isUnc;
    }

    /// <summary>
    /// Pulls the leading executable path out of an uninstall command string, handling
    /// the quoted-path-plus-arguments shape (<c>"C:\...\uninst.exe" /S</c>) as well as
    /// a bare unquoted path. Returns null when the shape doesn't look like a plain exe
    /// invocation (e.g. <c>MsiExec.exe /X{GUID}</c>, which isn't a path to check).
    /// </summary>
    public static string? ExtractLeadingExecutablePath(string? commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return null;
        }

        var trimmed = commandLine.Trim();

        string candidate;
        if (trimmed.StartsWith('"'))
        {
            var closingQuote = trimmed.IndexOf('"', 1);
            if (closingQuote < 0)
            {
                return null;
            }

            candidate = trimmed[1..closingQuote];
        }
        else
        {
            var firstSpace = trimmed.IndexOf(' ');
            candidate = firstSpace < 0 ? trimmed : trimmed[..firstSpace];
        }

        return LooksLikeExecutablePath(candidate) ? candidate : null;
    }
}
