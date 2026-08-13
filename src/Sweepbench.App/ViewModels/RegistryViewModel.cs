using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sweepbench.Core.Engine;
using Sweepbench.Core.Models;

namespace Sweepbench.App.ViewModels;

/// <summary>
/// Registry screen: same scan → select → clean shape as Health Check, but scoped to
/// MRU history and orphaned entries, and measured in issue count rather than bytes —
/// registry cleanup was never about reclaiming disk space. Every deletion is backed
/// up to a JSON undo log before it happens (see RegistryBackupWriter in Core).
/// </summary>
public sealed partial class RegistryViewModel : ObservableObject
{
    private readonly ScanEngine _scanEngine = ScanEngine.CreateRegistryScan();
    private readonly CleanExecutor _cleanExecutor = new();

    public ObservableCollection<CleanItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready to scan.";

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _totalCount;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusText = "Scanning the registry…";

        foreach (var vm in Items)
        {
            vm.SelectionChanged -= OnItemSelectionChanged;
        }
        Items.Clear();

        try
        {
            var result = await _scanEngine.ScanAsync();
            foreach (var item in result.Items.OrderByDescending(i => i.Risk == RiskLevel.Safe).ThenBy(i => i.DisplayName))
            {
                var vm = new CleanItemViewModel(item);
                vm.SelectionChanged += OnItemSelectionChanged;
                Items.Add(vm);
            }

            TotalCount = Items.Count;
            RecalculateSelection();
            StatusText = $"Scan complete — {Items.Count} issue(s) found in {result.Duration.TotalSeconds:0.0}s.";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            CleanCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanScan() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanClean))]
    private async Task CleanAsync()
    {
        var selected = Items.Where(i => i.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        IsBusy = true;
        var fixedCount = 0;
        var failedCount = 0;

        try
        {
            var progress = new Progress<CleanProgress>(p =>
                StatusText = $"Fixing {p.Completed}/{p.Total} — {p.CurrentItem.DisplayName}");

            var outcomes = await _cleanExecutor.ExecuteAsync(selected.Select(vm => vm.Item), progress);

            foreach (var outcome in outcomes)
            {
                if (outcome.Success)
                {
                    fixedCount++;
                    var vm = Items.FirstOrDefault(i => i.Item.Id == outcome.Item.Id);
                    if (vm is not null)
                    {
                        vm.SelectionChanged -= OnItemSelectionChanged;
                        Items.Remove(vm);
                    }
                }
                else
                {
                    failedCount++;
                }
            }

            StatusText = failedCount == 0
                ? $"Fixed {fixedCount} issue(s)."
                : $"Fixed {fixedCount} issue(s) — {failedCount} skipped.";

            TotalCount = Items.Count;
            RecalculateSelection();
        }
        finally
        {
            IsBusy = false;
            CleanCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanClean() => !IsBusy && SelectedCount > 0;

    private void OnItemSelectionChanged(object? sender, EventArgs e) => RecalculateSelection();

    private void RecalculateSelection()
    {
        SelectedCount = Items.Count(i => i.IsSelected);
        CleanCommand.NotifyCanExecuteChanged();
    }
}
