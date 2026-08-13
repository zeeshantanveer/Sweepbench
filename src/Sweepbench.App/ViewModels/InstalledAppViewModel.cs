using Sweepbench.App.Formatting;
using Sweepbench.Core.Models;

namespace Sweepbench.App.ViewModels;

/// <summary>Read-only display wrapper — nothing here changes after a scan, so no ObservableObject needed.</summary>
public sealed class InstalledAppViewModel
{
    public InstalledAppViewModel(InstalledApp app)
    {
        App = app;
    }

    public InstalledApp App { get; }

    public string DisplayName => App.DisplayName;

    public string Publisher => string.IsNullOrWhiteSpace(App.Publisher) ? "Unknown publisher" : App.Publisher;

    public string Version => App.Version ?? string.Empty;

    public string SizeText => App.EstimatedSizeBytes > 0 ? ByteFormatter.Format(App.EstimatedSizeBytes) : "—";
}
