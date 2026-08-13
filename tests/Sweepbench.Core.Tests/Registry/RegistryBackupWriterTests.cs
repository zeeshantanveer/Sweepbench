using System.Text.Json;
using Sweepbench.Core.Models;
using Sweepbench.Core.Registry;
using Sweepbench.Core.Tests.TestSupport;

namespace Sweepbench.Core.Tests.Registry;

public class RegistryBackupWriterTests
{
    [Fact]
    public void BackupBeforeDelete_SingleValue_CapturesNameKindAndData()
    {
        using var testKey = new TestRegistryKey();
        testKey.SetValue("Greeting", "hello", Microsoft.Win32.RegistryValueKind.String);
        using var backupDir = new TempDirectory();
        var writer = new RegistryBackupWriter(backupDir.Path);

        var target = new RegistryTarget(RegistryHive.CurrentUser, testKey.SubKeyPath, "Greeting");
        var path = writer.BackupBeforeDelete(target);

        var entry = JsonSerializer.Deserialize<RegistryBackupEntry>(File.ReadAllText(path));
        Assert.NotNull(entry);
        Assert.Equal("Greeting", entry!.ValueName);
        Assert.Equal("String", entry.ValueKind);
        Assert.Equal("hello", entry.Value);
    }

    [Fact]
    public void BackupBeforeDelete_WholeSubKey_CapturesAllValues()
    {
        using var testKey = new TestRegistryKey();
        testKey.SetValue("A", "1", Microsoft.Win32.RegistryValueKind.String);
        testKey.SetValue("B", "2", Microsoft.Win32.RegistryValueKind.String);
        using var backupDir = new TempDirectory();
        var writer = new RegistryBackupWriter(backupDir.Path);

        var target = new RegistryTarget(RegistryHive.CurrentUser, testKey.SubKeyPath, ValueName: null);
        var path = writer.BackupBeforeDelete(target);

        var entry = JsonSerializer.Deserialize<RegistryBackupEntry>(File.ReadAllText(path));
        Assert.NotNull(entry);
        Assert.Null(entry!.ValueName);
        Assert.Equal(2, entry.SubKeyValues.Count);
        Assert.Contains(entry.SubKeyValues, v => v.Name == "A" && v.Value == "1");
        Assert.Contains(entry.SubKeyValues, v => v.Name == "B" && v.Value == "2");
    }

    [Fact]
    public void BackupBeforeDelete_MissingKey_DoesNotThrow()
    {
        using var backupDir = new TempDirectory();
        var writer = new RegistryBackupWriter(backupDir.Path);
        var target = new RegistryTarget(RegistryHive.CurrentUser, @"Software\SweepbenchTests\does-not-exist", ValueName: null);

        var path = writer.BackupBeforeDelete(target);

        Assert.True(File.Exists(path));
    }
}
