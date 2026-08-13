using Sweepbench.Core.Cleaners;
using Sweepbench.Core.Models;

namespace Sweepbench.Core.Tests.TestSupport;

internal sealed class FakeCleaner : ICleaner
{
    private readonly IReadOnlyList<CleanItem> _items;
    private readonly Exception? _throws;

    public FakeCleaner(string id, IReadOnlyList<CleanItem> items)
    {
        Id = id;
        _items = items;
    }

    public FakeCleaner(string id, Exception throws)
    {
        Id = id;
        _items = [];
        _throws = throws;
    }

    public string Id { get; }

    public string DisplayName => Id;

    public CleanCategory Category => CleanCategory.TempFiles;

    public Task<IReadOnlyList<CleanItem>> ScanAsync(CancellationToken cancellationToken = default)
    {
        if (_throws is not null)
        {
            throw _throws;
        }

        return Task.FromResult(_items);
    }
}
