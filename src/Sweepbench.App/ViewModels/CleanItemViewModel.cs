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

    public string SizeText => ByteFormatter.Format(Item.SizeBytes);

    public long SizeBytes => Item.SizeBytes;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke(this, EventArgs.Empty);

    public event EventHandler? SelectionChanged;
}
