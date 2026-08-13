using CommunityToolkit.Mvvm.ComponentModel;
using Sweepbench.App.Formatting;
using Sweepbench.Core.Models;

namespace Sweepbench.App.ViewModels;

/// <summary>Wraps a read-only <see cref="CleanItem"/> with the selection state the UI needs.</summary>
public sealed partial class CleanItemViewModel : ObservableObject
{
    public CleanItemViewModel(CleanItem item)
    {
        Item = item;
        _isSelected = item.Risk == RiskLevel.Safe;
    }

    public CleanItem Item { get; }

    [ObservableProperty]
    private bool _isSelected;

    public string DisplayName => Item.DisplayName;

    public string Description => Item.Description;

    // Registry items carry a symbolic weight (value count), not real disk bytes —
    // formatting that as "3 B" would misrepresent it, so it gets its own label.
    public string SizeText => Item.Category == CleanCategory.Registry
        ? $"{Item.SizeBytes} value{(Item.SizeBytes == 1 ? "" : "s")}"
        : ByteFormatter.Format(Item.SizeBytes);

    public long SizeBytes => Item.SizeBytes;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? SelectionChanged;
}
