using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sweepbench.App.Formatting;
using Sweepbench.Core.Engine;
using Sweepbench.Core.Models;

namespace Sweepbench.App.ViewModels;

/// <summary>
/// Drives the Health Check screen: scan, let the user adjust the selection,
/// then clean only what's checked. Registry/Startup/Uninstall/Duplicates/Disk
/// Map/Erase are Phase 2+ — their nav entries exist but aren't wired up yet.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ScanEngine _scanEngine = ScanEngine.CreateDefault();
    private readonly CleanExecutor _cleanExecutor = new();

    public ObservableCollection<CleanItemViewModel> Items { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Ready to scan.";

    [ObservableProperty]
    private long _selectedBytes;

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private double _freeSpaceGb;

    [ObservableProperty]
    private double _totalSpaceGb;

    public string SelectedSizeText => ByteFormatter.Format(SelectedBytes);

    public string FreeSpaceText => $"{FreeSpaceGb:0.0} GB free of {TotalSpaceGb:0.0} GB";

    public MainViewModel()
    {
        RefreshDiskSpace();
    }

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        IsBusy = true;
        StatusText = "Scanning…";

        foreach (var vm in Items)
        {
            vm.SelectionChanged -= OnItemSelectionChanged;
        }
        Items.Clear();

        try
        {
            var result = await _scanEngine.ScanAsync();
            foreach (var item in result.Items.OrderByDescending(i => i.SizeBytes))
            {
                var vm = new CleanItemViewModel(item);
                vm.SelectionChanged += OnItemSelectionChanged;
                Items.Add(vm);
            }

            TotalCount = Items.Count;
            TotalBytes = result.TotalBytes;
            RecalculateSelection();
            StatusText = $"Scan complete — {Items.Count} item(s) found in {result.Duration.TotalSeconds:0.0}s.";
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
        var freedBytes = 0L;
        var failedCount = 0;

        try
        {
            var progress = new Progress<CleanProgress>(p =>
                StatusText = $"Cleaning {p.Completed}/{p.Total} — {p.CurrentItem.DisplayName}");

            var outcomes = await _cleanExecutor.ExecuteAsync(selected.Select(vm => vm.Item), progress);

            foreach (var outcome in outcomes)
            {
                if (outcome.Success)
                {
                    freedBytes += outcome.BytesFreed;
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
                ? $"Freed {ByteFormatter.Format(freedBytes)}."
                : $"Freed {ByteFormatter.Format(freedBytes)} — {failedCount} item(s) skipped (in use).";

            TotalCount = Items.Count;
            TotalBytes = Items.Sum(i => i.SizeBytes);
            RecalculateSelection();
            RefreshDiskSpace();
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
        SelectedBytes = Items.Where(i => i.IsSelected).Sum(i => i.SizeBytes);
        OnPropertyChanged(nameof(SelectedSizeText));
        CleanCommand.NotifyCanExecuteChanged();
    }

    private void RefreshDiskSpace()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var drive = new DriveInfo(root);
            FreeSpaceGb = drive.AvailableFreeSpace / 1024d / 1024 / 1024;
            TotalSpaceGb = drive.TotalSize / 1024d / 1024 / 1024;
            OnPropertyChanged(nameof(FreeSpaceText));
        }
        catch
        {
            // Disk-space header is informational only — never block scan/clean over it.
        }
    }
}
