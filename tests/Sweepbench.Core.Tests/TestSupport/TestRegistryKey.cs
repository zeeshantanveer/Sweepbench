using Microsoft.Win32;

namespace Sweepbench.Core.Tests.TestSupport;

/// <summary>
/// A scratch key under HKCU\Software\SweepbenchTests, deleted on dispose. HKCU is
/// used (never HKLM) so tests never need elevation and never touch anything the
/// running user didn't create themselves.
/// </summary>
internal sealed class TestRegistryKey : IDisposable
{
    private const string RootPath = @"Software\SweepbenchTests";

    public TestRegistryKey()
    {
        SubKeyPath = $"{RootPath}\\{Guid.NewGuid():N}";
        // Fully-qualified: this project also has a "Sweepbench.Core.Tests.Registry"
        // namespace (for the registry tests folder), which makes a bare `Registry.*`
        // reference here ambiguous.
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(SubKeyPath, writable: true);
    }

    /// <summary>Relative to HKCU, e.g. "Software\SweepbenchTests\&lt;guid&gt;".</summary>
    public string SubKeyPath { get; }

    public void SetValue(string name, object value, RegistryValueKind kind)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SubKeyPath, writable: true);
        key!.SetValue(name, value, kind);
    }

    public RegistryKey? Open(bool writable = false) => Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SubKeyPath, writable);

    public void Dispose()
    {
        try
        {
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(SubKeyPath, throwOnMissingSubKey: false);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
