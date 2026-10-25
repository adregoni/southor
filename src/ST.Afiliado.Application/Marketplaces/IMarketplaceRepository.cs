using ST.Afiliado.Domain.Marketplaces;

namespace ST.Afiliado.Application.Marketplaces;

public interface IMarketplaceRepository
{
    Task<IReadOnlyList<Marketplace>> ListAsync(CancellationToken cancellationToken = default);
    Task<Marketplace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Marketplace marketplace, CancellationToken cancellationToken = default);
    Task UpdateAsync(Marketplace marketplace, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
