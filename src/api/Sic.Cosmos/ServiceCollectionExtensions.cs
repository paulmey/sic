using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Sic.Core.Repositories;
using Sic.Cosmos.Repositories;

namespace Sic.Cosmos;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCosmosRepositories(
        this IServiceCollection services, string connectionString,
        string databaseName = "sic", string containerName = "sic-data")
    {
        services.AddSingleton(sp =>
        {
            var client = new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
            return client;
        });

        services.AddScoped<IUserRepository>(sp =>
            new CosmosUserRepository(sp.GetRequiredService<CosmosClient>(), databaseName, containerName));
        services.AddScoped<ICategoryRepository>(sp =>
            new CosmosCategoryRepository(sp.GetRequiredService<CosmosClient>(), databaseName, containerName));
        services.AddScoped<IResourceRepository>(sp =>
            new CosmosResourceRepository(sp.GetRequiredService<CosmosClient>(), databaseName, containerName));
        services.AddScoped<IResourceRoleRepository>(sp =>
            new CosmosResourceRoleRepository(sp.GetRequiredService<CosmosClient>(), databaseName, containerName));
        services.AddScoped<IBookingRepository>(sp =>
            new CosmosBookingRepository(sp.GetRequiredService<CosmosClient>(), databaseName, containerName));
        services.AddScoped<IInviteLinkRepository>(sp =>
            new CosmosInviteLinkRepository(sp.GetRequiredService<CosmosClient>(), databaseName, containerName));

        return services;
    }
}
