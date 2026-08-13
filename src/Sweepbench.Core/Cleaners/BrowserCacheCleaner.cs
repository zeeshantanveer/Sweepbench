using Sweepbench.Core.Models;

namespace Sweepbench.Core.Cleaners;

public sealed class BrowserCacheCleaner : ICleaner
{
    // Each browser stores cache under its default profile; users with multiple
    // profiles keep the rest untouched — Phase 2 can add a profile enumerator.
    private static readonly (string Browser, string RelativePath)[] Targets =
    [
        ("Google Chrome", @"Google\Chrome\User Data\Default\Cache"),
        ("Google Chrome", @"Google\Chrome\User Data\Default\Code Cache"),
        ("Microsoft Edge", @"Microsoft\Edge\User Data\Default\Cache"),
        ("Microsoft Edge", @"Microsoft\Edge\User Data\Default\Code Cache"),
        ("Mozilla Firefox", @"Mozilla\Firefox\Profiles"),
    ];

    public string Id => "browser-cache";

    public string DisplayName => "Browser Cache";

    public CleanCategory Category => CleanCategory.BrowserCache;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var items = new List<CleanItem>();

        foreach (var (browser, relative) in Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (browser == "Mozilla Firefox")
            {
                items.AddRange(ScanFirefoxProfiles(Path.Combine(localAppData, relative)));
                continue;
            }

            var path = Path.Combine(localAppData, relative);
            if (!Directory.Exists(path))
            {
                continue;
            }

            var size = DirectorySize.Calculate(path);
            if (size <= 0)
            {
                continue;
            }

            items.Add(new CleanItem(
                Id: $"browser-cache:{path}",
                DisplayName: $"{browser} cache",
                Description: path,
                Path: path,
                SizeBytes: size,
                Category: CleanCategory.BrowserCache,
                Risk: RiskLevel.Safe,
                Kind: CleanItemKind.Directory));
        }

        return Task.FromResult<IReadOnlyList<CleanItem>>(items);
    }

    // Firefox nests cache under a randomly-named profile folder, e.g.
    // Profiles\abcd1234.default-release\cache2 — has to be discovered, not guessed.
    private static IEnumerable<CleanItem> ScanFirefoxProfiles(string profilesRoot)
    {
        if (!Directory.Exists(profilesRoot))
        {
            yield break;
        }

        string[] profileDirs;
        try
        {
            profileDirs = Directory.GetDirectories(profilesRoot);
        }
        catch
        {
            yield break;
        }

        foreach (var profileDir in profileDirs)
        {
            var cachePath = Path.Combine(profileDir, "cache2");
            if (!Directory.Exists(cachePath))
            {
                continue;
            }

            var size = DirectorySize.Calculate(cachePath);
            if (size <= 0)
            {
                continue;
            }

            yield return new CleanItem(
                Id: $"browser-cache:{cachePath}",
                DisplayName: $"Firefox cache ({Path.GetFileName(profileDir)})",
                Description: cachePath,
                Path: cachePath,
                SizeBytes: size,
                Category: CleanCategory.BrowserCache,
                Risk: RiskLevel.Safe,
                Kind: CleanItemKind.Directory);
        }
    }
}
