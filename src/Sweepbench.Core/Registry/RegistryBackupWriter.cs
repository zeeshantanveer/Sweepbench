using System.Text.Json;
using Microsoft.Win32;
using Sweepbench.Core.Models;

namespace Sweepbench.Core.Registry;

/// <summary>
/// Writes every value or subkey about to be deleted to a timestamped JSON log before
/// the delete happens. This is Phase 2's safety net in place of a full System Restore
/// point integration (tracked as a follow-up — see README) — cheap, always available
/// even when System Restore is disabled, and enough to manually reconstruct what was
/// removed.
/// </summary>
public sealed class RegistryBackupWriter
{
    private readonly string _backupDirectory;

    public RegistryBackupWriter(string? backupDirectory = null)
    {
        _backupDirectory = backupDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sweepbench", "registry-backups");
    }

    public string BackupBeforeDelete(RegistryTarget target)
    {
        Directory.CreateDirectory(_backupDirectory);

        var entry = target.ValueName is null
            ? CaptureSubKey(target)
            : CaptureValue(target);

        var fileName = $"{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
        var path = Path.Combine(_backupDirectory, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    private static RegistryBackupEntry CaptureValue(RegistryTarget target)
    {
        using var key = OpenReadOnly(target);
        if (key is null)
        {
            return new RegistryBackupEntry(target.Hive, target.SubKeyPath, target.ValueName, null, null, []);
        }

        var value = key.GetValue(target.ValueName);
        var kind = key.GetValueKind(target.ValueName!);
        return new RegistryBackupEntry(target.Hive, target.SubKeyPath, target.ValueName, kind.ToString(), Encode(value, kind), []);
    }

    private static RegistryBackupEntry CaptureSubKey(RegistryTarget target)
    {
        using var key = OpenReadOnly(target);
        if (key is null)
        {
            return new RegistryBackupEntry(target.Hive, target.SubKeyPath, null, null, null, []);
        }

        var values = new List<RegistryBackupValue>();
        foreach (var valueName in key.GetValueNames())
        {
            var kind = key.GetValueKind(valueName);
            var value = key.GetValue(valueName);
            values.Add(new RegistryBackupValue(valueName, kind.ToString(), Encode(value, kind)));
        }

        return new RegistryBackupEntry(target.Hive, target.SubKeyPath, null, null, null, values);
    }

    private static RegistryKey? OpenReadOnly(RegistryTarget target) =>
        target.Hive.ToBaseKey().OpenSubKey(target.SubKeyPath, writable: false);

    // REG_BINARY needs base64; everything else round-trips through JSON as-is.
    private static string? Encode(object? value, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.Binary => Convert.ToBase64String(value as byte[] ?? []),
        RegistryValueKind.MultiString => JsonSerializer.Serialize(value as string[] ?? []),
        _ => value?.ToString(),
    };
}

public sealed record RegistryBackupValue(string Name, string Kind, string? Value);

public sealed record RegistryBackupEntry(
    Sweepbench.Core.Models.RegistryHive Hive,
    string SubKeyPath,
    string? ValueName,
    string? ValueKind,
    string? Value,
    List<RegistryBackupValue> SubKeyValues);
