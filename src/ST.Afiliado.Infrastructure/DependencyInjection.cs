using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ST.Afiliado.Application.Marketplaces;
using ST.Afiliado.Infrastructure.Marketplaces;

namespace ST.Afiliado.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var connectionString = configuration["MongoDb:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IMarketplaceRepository, InMemoryMarketplaceRepository>();
            services.AddSingleton<IMarketplacePersistenceStatus>(new MarketplacePersistenceStatus(false));
        }
        else
        {
            var databaseName = configuration["MongoDb:DatabaseName"] ?? "southor_afiliados";
            services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
            services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
            services.AddSingleton<IMarketplaceRepository, MongoMarketplaceRepository>();
            services.AddSingleton<IMarketplacePersistenceStatus>(new MarketplacePersistenceStatus(true));
        }
        services.AddScoped<MarketplaceService>();
        return services;
    }
}

internal sealed record MarketplacePersistenceStatus(bool IsPersistent) : IMarketplacePersistenceStatus;
