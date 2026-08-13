using Sweepbench.Core.Engine;
using Sweepbench.Core.Models;
using Sweepbench.Core.Tests.TestSupport;

namespace Sweepbench.Core.Tests.Engine;

public class CleanExecutorRegistryTests
{
    private static CleanItem RegistryItem(RegistryTarget target) => new(
        Id: "test:registry",
        DisplayName: "Test entry",
        Description: "Test entry",
        Path: $@"HKCU\{target.SubKeyPath}",
        SizeBytes: 1,
        Category: CleanCategory.Registry,
        Risk: RiskLevel.Safe,
        Kind: CleanItemKind.RegistryValue,
        Registry: target);

    [Fact]
    public async Task ExecuteAsync_RegistryValue_DeletesOnlyThatValue()
    {
        using var testKey = new TestRegistryKey();
        testKey.SetValue("KeepMe", "1", Microsoft.Win32.RegistryValueKind.String);
        testKey.SetValue("DeleteMe", "2", Microsoft.Win32.RegistryValueKind.String);

        var target = new RegistryTarget(RegistryHive.CurrentUser, testKey.SubKeyPath, "DeleteMe");
        var executor = new CleanExecutor();

        var outcomes = await executor.ExecuteAsync([RegistryItem(target)]);

        var outcome = Assert.Single(outcomes);
        Assert.True(outcome.Success);

        using var key = testKey.Open();
        Assert.Null(key!.GetValue("DeleteMe"));
        Assert.Equal("1", key.GetValue("KeepMe"));
    }

    [Fact]
    public async Task ExecuteAsync_RegistrySubKey_DeletesWholeSubKey()
    {
        using var testKey = new TestRegistryKey();
        var childPath = $"{testKey.SubKeyPath}\\Child";
        using (var child = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(childPath, writable: true))
        {
            child!.SetValue("X", "1");
        }

        var target = new RegistryTarget(RegistryHive.CurrentUser, childPath, ValueName: null);
        var executor = new CleanExecutor();

        var outcomes = await executor.ExecuteAsync([RegistryItem(target)]);

        var outcome = Assert.Single(outcomes);
        Assert.True(outcome.Success);
        Assert.Null(Microsoft.Win32.Registry.CurrentUser.OpenSubKey(childPath));
    }

    [Fact]
    public async Task ExecuteAsync_RegistryItemWithoutTarget_FailsGracefully()
    {
        var item = new CleanItem(
            Id: "test:no-target",
            DisplayName: "Broken",
            Description: "Broken",
            Path: string.Empty,
            SizeBytes: 1,
            Category: CleanCategory.Registry,
            Risk: RiskLevel.Safe,
            Kind: CleanItemKind.RegistryValue,
            Registry: null);

        var executor = new CleanExecutor();
        var outcomes = await executor.ExecuteAsync([item]);

        var outcome = Assert.Single(outcomes);
        Assert.False(outcome.Success);
        Assert.NotNull(outcome.Error);
    }
}
