namespace Sweepbench.Core.Tests.TestSupport;

/// <summary>A scratch directory under the OS temp folder, cleaned up when disposed.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sweepbench-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string WriteFile(string relativeName, int sizeBytes)
    {
        var fullPath = System.IO.Path.Combine(Path, relativeName);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllBytes(fullPath, new byte[sizeBytes]);
        return fullPath;
    }

    public string CreateSubdirectory(string relativeName)
    {
        var fullPath = System.IO.Path.Combine(Path, relativeName);
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup — a leftover temp folder isn't worth failing the test run over.
        }
    }
}
