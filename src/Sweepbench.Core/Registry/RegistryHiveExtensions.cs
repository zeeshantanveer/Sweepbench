using Microsoft.Win32;

namespace Sweepbench.Core.Registry;

internal static class RegistryHiveExtensions
{
    // NB: always fully-qualify Sweepbench.Core.Models.RegistryHive and
    // Microsoft.Win32.Registry.CurrentUser/LocalMachine at call sites — both
    // "RegistryHive" and "Registry" collide with BCL types of the same name.
    public static RegistryKey ToBaseKey(this Sweepbench.Core.Models.RegistryHive hive) =>
        hive == Sweepbench.Core.Models.RegistryHive.CurrentUser
            ? Microsoft.Win32.Registry.CurrentUser
            : Microsoft.Win32.Registry.LocalMachine;
}
