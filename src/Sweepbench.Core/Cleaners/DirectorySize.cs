namespace Sweepbench.Core.Cleaners;

internal static class DirectorySize
{
    /// <summary>
    /// Best-effort recursive size. Locked files, permission errors, and races with
    /// other processes deleting files mid-walk are swallowed — a partial total beats
    /// failing the whole scan over one unreadable file.
    /// </summary>
    public static long Calculate(string path)
    {
        long total = 0;

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
        }
        catch
        {
            return 0;
        }

        try
        {
            foreach (var file in files)
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip files that vanish or lock between enumeration and stat.
                }
            }
        }
        catch
        {
            // Enumeration itself can throw partway through (e.g. a subfolder ACL
            // denies listing) — return whatever we accumulated so far.
        }

        return total;
    }
}
