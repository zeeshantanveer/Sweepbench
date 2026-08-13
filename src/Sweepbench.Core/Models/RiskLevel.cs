namespace Sweepbench.Core.Models;

/// <summary>
/// How much scrutiny an item deserves before it's cleaned.
/// Phase 1 cleaners only ever produce <see cref="Safe"/> items — registry and
/// uninstall actions in later phases will introduce <see cref="Caution"/>.
/// </summary>
public enum RiskLevel
{
    Safe,
    Caution,
}
