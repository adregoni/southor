namespace ST.Afiliado.Domain.Marketplaces;

public sealed class Marketplace
{
    private Marketplace() { }

    public Marketplace(Guid id, MarketplaceType type, string name, string affiliateCode, bool isActive)
    {
        if (id == Guid.Empty) throw new ArgumentException("O identificador é obrigatório.", nameof(id));
        Id = id;
        Update(type, name, affiliateCode, isActive);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public MarketplaceType Type { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string AffiliateCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Marketplace Reconstitute(Guid id, MarketplaceType type, string name, string affiliateCode, bool isActive, DateTime createdAtUtc, DateTime updatedAtUtc)
    {
        var marketplace = new Marketplace(id, type, name, affiliateCode, isActive)
        {
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };
        return marketplace;
    }

    public void Update(MarketplaceType type, string name, string affiliateCode, bool isActive)
    {
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type), "Marketplace não suportado.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            throw new ArgumentException("Informe um nome de até 80 caracteres.", nameof(name));
        if (string.IsNullOrWhiteSpace(affiliateCode) || affiliateCode.Trim().Length > 200)
            throw new ArgumentException("Informe o identificador de afiliado (até 200 caracteres).", nameof(affiliateCode));

        Type = type;
        Name = name.Trim();
        AffiliateCode = affiliateCode.Trim();
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
