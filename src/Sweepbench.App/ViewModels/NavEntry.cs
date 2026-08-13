namespace Sweepbench.App.ViewModels;

public sealed class NavEntry
{
    public NavEntry(string label, object? viewModel, bool isEnabled, string? badge = null)
    {
        Label = label;
        ViewModel = viewModel;
        IsEnabled = isEnabled;
        Badge = badge;
    }

    public string Label { get; }

    public object? ViewModel { get; }

    public bool IsEnabled { get; }

    /// <summary>e.g. "Phase 3" for the not-yet-built screens.</summary>
    public string? Badge { get; }
}
