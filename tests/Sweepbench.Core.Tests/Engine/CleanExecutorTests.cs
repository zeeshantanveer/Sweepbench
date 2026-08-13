using Sweepbench.Core.Engine;
using Sweepbench.Core.Models;
using Sweepbench.Core.Tests.TestSupport;

namespace Sweepbench.Core.Tests.Engine;

public class CleanExecutorTests
{
    private static CleanItem FileItem(string path, long size) => new(
        Id: $"test:{path}",
        DisplayName: System.IO.Path.GetFileName(path),
        Description: path,
        Path: path,
        SizeBytes: size,
        Category: CleanCategory.TempFiles,
        Risk: RiskLevel.Safe,
        Kind: CleanItemKind.File);

    private static CleanItem DirectoryItem(string path, long size) => new(
        Id: $"test:{path}",
        DisplayName: System.IO.Path.GetFileName(path),
        Description: path,
        Path: path,
        SizeBytes: size,
        Category: CleanCategory.TempFiles,
        Risk: RiskLevel.Safe,
        Kind: CleanItemKind.Directory);

    [Fact]
    public async Task ExecuteAsync_File_SendsToRecycleBinAndReportsFreedBytes()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("delete-me.txt", 512);
        var item = FileItem(path, 512);
        var executor = new CleanExecutor();

        var outcomes = await executor.ExecuteAsync([item]);

        var outcome = Assert.Single(outcomes);
        Assert.True(outcome.Success);
        Assert.Equal(512, outcome.BytesFreed);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ExecuteAsync_Directory_SendsToRecycleBinAndReportsFreedBytes()
    {
        using var dir = new TempDirectory();
        var subPath = dir.CreateSubdirectory("delete-me-dir");
        dir.WriteFile(System.IO.Path.Combine("delete-me-dir", "a.txt"), 300);
        var item = DirectoryItem(subPath, 300);
        var executor = new CleanExecutor();

        var outcomes = await executor.ExecuteAsync([item]);

        var outcome = Assert.Single(outcomes);
        Assert.True(outcome.Success);
        Assert.Equal(300, outcome.BytesFreed);
        Assert.False(Directory.Exists(subPath));
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyGoneFile_TreatedAsSuccessWithNoBytesFreed()
    {
        using var dir = new TempDirectory();
        var path = System.IO.Path.Combine(dir.Path, "never-existed.txt");
        var item = FileItem(path, 999);
        var executor = new CleanExecutor();

        var outcomes = await executor.ExecuteAsync([item]);

        var outcome = Assert.Single(outcomes);
        Assert.True(outcome.Success);
        Assert.Equal(0, outcome.BytesFreed);
    }

    [Fact]
    public async Task ExecuteAsync_ReportsProgressForEachItem()
    {
        using var dir = new TempDirectory();
        var itemA = FileItem(dir.WriteFile("a.txt", 10), 10);
        var itemB = FileItem(dir.WriteFile("b.txt", 20), 20);
        var executor = new CleanExecutor();
        var reports = new List<CleanProgress>();
        var progress = new Progress<CleanProgress>(reports.Add);

        await executor.ExecuteAsync([itemA, itemB], progress);

        // Progress<T> marshals via the sync context, which may not have flushed by the
        // time ExecuteAsync returns on a plain thread-pool context — pump briefly.
        for (var i = 0; i < 20 && reports.Count < 2; i++)
        {
            await Task.Delay(10);
        }

        Assert.Equal(2, reports.Count);
        Assert.Equal(2, reports[^1].Total);
    }
}
