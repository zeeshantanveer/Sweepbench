using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sweepbench.Core.Startup;

namespace Sweepbench.App.ViewModels;

public sealed partial class StartupViewModel : ObservableObject
{
    private readonly StartupScanner _scanner = new();
    private readonly StartupToggleService _toggleService = new();

    public ObservableCollection<StartupItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready to scan.";

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusText = "Scanning startup items…";
        Items.Clear();

        try
        {
            var result = await _scanner.ScanAsync();
            foreach (var item in result.OrderBy(i => i.Name))
            {
                Items.Add(new StartupItemViewModel(item, _toggleService));
            }

            StatusText = $"{Items.Count} startup item(s) found.";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanScan() => !IsBusy;
}
