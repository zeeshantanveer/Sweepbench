using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sweepbench.App.ViewModels;

/// <summary>Owns the sidebar and switches the main content between the implemented screens.</summary>
public sealed partial class ShellViewModel : ObservableObject
{
    public MainViewModel HealthCheck { get; } = new();

    public RegistryViewModel Registry { get; } = new();

    public StartupViewModel StartupManager { get; } = new();

    public UninstallViewModel Uninstall { get; } = new();

    public ObservableCollection<NavEntry> NavItems { get; }

    [ObservableProperty]
    private NavEntry _selectedNavItem;

    [ObservableProperty]
    private object _currentView;

    public ShellViewModel()
    {
        NavItems =
        [
            new NavEntry("Health Check", HealthCheck, isEnabled: true),
            new NavEntry("Registry", Registry, isEnabled: true),
            new NavEntry("Startup", StartupManager, isEnabled: true),
            new NavEntry("Uninstall", Uninstall, isEnabled: true),
            new NavEntry("Duplicates", null, isEnabled: false, badge: "Phase 3"),
            new NavEntry("Disk Map", null, isEnabled: false, badge: "Phase 3"),
            new NavEntry("Erase", null, isEnabled: false, badge: "Phase 3"),
        ];

        _selectedNavItem = NavItems[0];
        _currentView = HealthCheck;
    }

    partial void OnSelectedNavItemChanged(NavEntry value)
    {
        if (value.IsEnabled && value.ViewModel is not null)
        {
            CurrentView = value.ViewModel;
        }
    }
}
