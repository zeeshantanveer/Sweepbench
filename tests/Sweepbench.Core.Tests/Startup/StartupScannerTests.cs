using Sweepbench.Core.Startup;

namespace Sweepbench.Core.Tests.Startup;

public class StartupScannerTests
{
    [Fact]
    public async Task ScanAsync_ReturnsItemsWithNonEmptyNames()
    {
        var scanner = new StartupScanner();

        var items = await scanner.ScanAsync();

        Assert.All(items, item => Assert.False(string.IsNullOrWhiteSpace(item.Name)));
    }

    [Fact]
    public async Task ScanAsync_NoDuplicateIds()
    {
        var scanner = new StartupScanner();

        var items = await scanner.ScanAsync();

        var ids = items.Select(i => i.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
