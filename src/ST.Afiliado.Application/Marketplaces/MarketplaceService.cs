using ST.Afiliado.Domain.Marketplaces;

namespace ST.Afiliado.Application.Marketplaces;

public sealed class MarketplaceService(IMarketplaceRepository repository)
{
    public Task<IReadOnlyList<Marketplace>> ListAsync(CancellationToken cancellationToken = default) => repository.ListAsync(cancellationToken);

    public async Task<Marketplace> CreateAsync(MarketplaceType type, string name, string affiliateCode, bool isActive, CancellationToken cancellationToken = default)
    {
        var marketplace = new Marketplace(Guid.NewGuid(), type, name, affiliateCode, isActive);
        await repository.AddAsync(marketplace, cancellationToken);
        return marketplace;
    }

    public async Task<bool> UpdateAsync(Guid id, MarketplaceType type, string name, string affiliateCode, bool isActive, CancellationToken cancellationToken = default)
    {
        var marketplace = await repository.GetByIdAsync(id, cancellationToken);
        if (marketplace is null) return false;
        marketplace.Update(type, name, affiliateCode, isActive);
        await repository.UpdateAsync(marketplace, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (await repository.GetByIdAsync(id, cancellationToken) is null) return false;
        await repository.DeleteAsync(id, cancellationToken);
        return true;
    }
}
