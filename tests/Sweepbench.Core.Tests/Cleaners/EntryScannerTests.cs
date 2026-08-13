using Sweepbench.Core.Cleaners;
using Sweepbench.Core.Models;
using Sweepbench.Core.Tests.TestSupport;

namespace Sweepbench.Core.Tests.Cleaners;

public class EntryScannerTests
{
    [Fact]
    public void ScanTopLevel_ReturnsOneItemPerTopLevelEntry()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("file.txt", 100);
        dir.CreateSubdirectory("folder");
        dir.WriteFile(System.IO.Path.Combine("folder", "nested.txt"), 200);

        var items = EntryScanner
            .ScanTopLevel(dir.Path, "Test root", CleanCategory.TempFiles, RiskLevel.Safe, "test")
            .OrderBy(i => i.DisplayName)
            .ToList();

        Assert.Equal(2, items.Count);

        var file = items.Single(i => i.DisplayName == "file.txt");
        Assert.Equal(CleanItemKind.File, file.Kind);
        Assert.Equal(100, file.SizeBytes);

        var folder = items.Single(i => i.DisplayName == "folder");
        Assert.Equal(CleanItemKind.Directory, folder.Kind);
        Assert.Equal(200, folder.SizeBytes);
    }

    [Fact]
    public void ScanTopLevel_SkipsEmptyEntries()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("empty.txt", 0);
        dir.CreateSubdirectory("empty-folder");

        var items = EntryScanner.ScanTopLevel(dir.Path, "Test root", CleanCategory.TempFiles, RiskLevel.Safe, "test");

        Assert.Empty(items);
    }

    [Fact]
    public void ScanTopLevel_NonexistentRoot_ReturnsEmpty()
    {
        var items = EntryScanner.ScanTopLevel(
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sweepbench-does-not-exist-" + Guid.NewGuid()),
            "Test root", CleanCategory.TempFiles, RiskLevel.Safe, "test");

        Assert.Empty(items);
    }

    [Fact]
    public void ScanTopLevel_SetsRequestedCategoryAndRisk()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("file.txt", 42);

        var item = EntryScanner
            .ScanTopLevel(dir.Path, "Label", CleanCategory.WindowsUpdateCache, RiskLevel.Caution, "wu")
            .Single();

        Assert.Equal(CleanCategory.WindowsUpdateCache, item.Category);
        Assert.Equal(RiskLevel.Caution, item.Risk);
        Assert.StartsWith("wu:", item.Id);
    }
}
