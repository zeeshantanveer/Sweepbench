using Sweepbench.Core.Engine;
using Sweepbench.Core.Models;
using Sweepbench.Core.Tests.TestSupport;

namespace Sweepbench.Core.Tests.Engine;

public class ScanEngineTests
{
    private static CleanItem MakeItem(string id, long size) => new(
        Id: id,
        DisplayName: id,
        Description: id,
        Path: id,
        SizeBytes: size,
        Category: CleanCategory.TempFiles,
        Risk: RiskLevel.Safe,
        Kind: CleanItemKind.File);

    [Fact]
    public async Task ScanAsync_MergesItemsFromAllCleaners()
    {
        var cleanerA = new FakeCleaner("a", [MakeItem("a1", 100)]);
        var cleanerB = new FakeCleaner("b", [MakeItem("b1", 200), MakeItem("b2", 50)]);
        var engine = new ScanEngine([cleanerA, cleanerB]);

        var result = await engine.ScanAsync();

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(350, result.TotalBytes);
    }

    [Fact]
    public async Task ScanAsync_OneCleanerThrowing_DoesNotFailOtherCleaners()
    {
        var broken = new FakeCleaner("broken", new InvalidOperationException("access denied"));
        var healthy = new FakeCleaner("healthy", [MakeItem("h1", 500)]);
        var engine = new ScanEngine([broken, healthy]);

        var result = await engine.ScanAsync();

        var item = Assert.Single(result.Items);
        Assert.Equal("h1", item.Id);
    }

    [Fact]
    public async Task ScanAsync_NoCleaners_ReturnsEmptyResult()
    {
        var engine = new ScanEngine([]);

        var result = await engine.ScanAsync();

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalBytes);
    }
}
