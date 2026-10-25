using ST.Afiliado.Application.Marketplaces;
using ST.Afiliado.Domain.Marketplaces;

namespace ST.Afiliado.Infrastructure.Marketplaces;

internal sealed class InMemoryMarketplaceRepository : IMarketplaceRepository
{
    private readonly Dictionary<Guid, Marketplace> _items = [];
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<Marketplace>> ListAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return _items.Values.OrderByDescending(item => item.CreatedAtUtc).ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<Marketplace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { return _items.GetValueOrDefault(id); }
        finally { _gate.Release(); }
    }

    public async Task AddAsync(Marketplace marketplace, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { _items.Add(marketplace.Id, marketplace); }
        finally { _gate.Release(); }
    }

    public async Task UpdateAsync(Marketplace marketplace, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { _items[marketplace.Id] = marketplace; }
        finally { _gate.Release(); }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { _items.Remove(id); }
        finally { _gate.Release(); }
    }
}
