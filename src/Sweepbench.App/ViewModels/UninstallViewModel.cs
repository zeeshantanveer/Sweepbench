using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sweepbench.Core.Uninstall;

namespace Sweepbench.App.ViewModels;

/// <summary>
/// Lists installed applications and hands off to each one's own uninstaller —
/// Sweepbench never attempts a "leftover file sweep" after (see AppUninstaller for
/// why that's out of scope, not just unimplemented).
/// </summary>
public sealed partial class UninstallViewModel : ObservableObject
{
    private readonly InstalledAppScanner _scanner = new();
    private readonly AppUninstaller _uninstaller = new();

    public ObservableCollection<InstalledAppViewModel> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready to scan.";

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusText = "Scanning installed applications…";
        Items.Clear();

        try
        {
            var result = await _scanner.ScanAsync();
            foreach (var app in result)
            {
                Items.Add(new InstalledAppViewModel(app));
            }

            StatusText = $"{Items.Count} application(s) found.";
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

    [RelayCommand]
    private void Uninstall(InstalledAppViewModel? app)
    {
        if (app is null)
        {
            return;
        }

        var outcome = _uninstaller.Launch(app.App);
        StatusText = outcome.Success
            ? $"Launched the uninstaller for {app.DisplayName}."
            : $"Couldn't launch uninstaller for {app.DisplayName}: {outcome.Error}";
    }
}
