using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sweepbench.Core.Models;
using Sweepbench.Core.Startup;

namespace Sweepbench.App.ViewModels;

public sealed partial class StartupItemViewModel : ObservableObject
{
    private readonly StartupToggleService _toggleService;

    public StartupItemViewModel(StartupItem item, StartupToggleService toggleService)
    {
        Item = item;
        _toggleService = toggleService;
        _isEnabled = item.IsEnabled;
    }

    public StartupItem Item { get; }

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string? _errorText;

    public string Name => Item.Name;

    public string Command => Item.Command;

    public string LocationLabel => Item.Location switch
    {
        StartupLocation.RunKeyCurrentUser => "Run key · this account",
        StartupLocation.RunKeyLocalMachine => "Run key · all users",
        StartupLocation.StartupFolderUser => "Startup folder · this account",
        StartupLocation.StartupFolderCommon => "Startup folder · all users",
        _ => string.Empty,
    };

    // Bound to the toggle's Command rather than reacting to IsEnabled changing —
    // that keeps a failed toggle a single, explicit no-op instead of a second
    // property-changed notification trying to "correct" the value.
    [RelayCommand]
    private void Toggle()
    {
        var target = !IsEnabled;
        var outcome = _toggleService.SetEnabled(Item, target);

        if (outcome.Success)
        {
            IsEnabled = target;
            ErrorText = null;
        }
        else
        {
            ErrorText = outcome.Error;
        }
    }
}
