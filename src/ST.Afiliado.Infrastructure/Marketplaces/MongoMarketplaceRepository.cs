using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ST.Afiliado.Application.Marketplaces;
using ST.Afiliado.Domain.Marketplaces;

namespace ST.Afiliado.Infrastructure.Marketplaces;

internal sealed class MongoMarketplaceRepository(IMongoDatabase database) : IMarketplaceRepository
{
    private readonly IMongoCollection<MarketplaceDocument> _collection = database.GetCollection<MarketplaceDocument>("marketplaces");

    public async Task<IReadOnlyList<Marketplace>> ListAsync(CancellationToken cancellationToken = default)
    {
        var documents = await _collection.Find(FilterDefinition<MarketplaceDocument>.Empty)
            .SortByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);
        return documents.Select(ToDomain).ToArray();
    }

    public async Task<Marketplace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(item => item.Id == id.ToString("D")).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task AddAsync(Marketplace marketplace, CancellationToken cancellationToken = default) =>
        await _collection.InsertOneAsync(ToDocument(marketplace), cancellationToken: cancellationToken);

    public async Task UpdateAsync(Marketplace marketplace, CancellationToken cancellationToken = default) =>
        await _collection.ReplaceOneAsync(item => item.Id == marketplace.Id.ToString("D"), ToDocument(marketplace), cancellationToken: cancellationToken);

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _collection.DeleteOneAsync(item => item.Id == id.ToString("D"), cancellationToken);

    private static MarketplaceDocument ToDocument(Marketplace item) => new()
    {
        Id = item.Id.ToString("D"), Type = item.Type, Name = item.Name, AffiliateCode = item.AffiliateCode,
        IsActive = item.IsActive, CreatedAtUtc = item.CreatedAtUtc, UpdatedAtUtc = item.UpdatedAtUtc
    };

    private static Marketplace ToDomain(MarketplaceDocument item)
    {
        return Marketplace.Reconstitute(Guid.Parse(item.Id), item.Type, item.Name, item.AffiliateCode,
            item.IsActive, item.CreatedAtUtc, item.UpdatedAtUtc);
    }

    private sealed class MarketplaceDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;
        public MarketplaceType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AffiliateCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
