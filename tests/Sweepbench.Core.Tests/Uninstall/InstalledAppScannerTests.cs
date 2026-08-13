using Sweepbench.Core.Uninstall;

namespace Sweepbench.Core.Tests.Uninstall;

public class InstalledAppScannerTests
{
    [Fact]
    public async Task ScanAsync_ReturnsAppsWithNonEmptyDisplayNameAndUninstallCommand()
    {
        var scanner = new InstalledAppScanner();

        var apps = await scanner.ScanAsync();

        Assert.All(apps, app =>
        {
            Assert.False(string.IsNullOrWhiteSpace(app.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(app.UninstallCommand));
        });
    }

    [Fact]
    public async Task ScanAsync_NoDuplicateIds()
    {
        var scanner = new InstalledAppScanner();

        var apps = await scanner.ScanAsync();

        var ids = apps.Select(a => a.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
