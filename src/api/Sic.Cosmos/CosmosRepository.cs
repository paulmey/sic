using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Sic.Cosmos;

public abstract class CosmosRepository
{
    protected readonly Container _container;
    protected abstract string TypeName { get; }

    protected CosmosRepository(CosmosClient client, string databaseName, string containerName)
    {
        _container = client.GetContainer(databaseName, containerName);
    }

    protected async Task<T?> ReadItemOrDefaultAsync<T>(string id, string pk) where T : class
    {
        try
        {
            var response = await _container.ReadItemAsync<T>(id, new PartitionKey(pk));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    protected async Task<List<T>> QueryAsync<T>(QueryDefinition query, string? partitionKey = null)
    {
        var options = new QueryRequestOptions();
        if (partitionKey is not null)
            options.PartitionKey = new PartitionKey(partitionKey);

        var results = new List<T>();
        using var iterator = _container.GetItemQueryIterator<T>(query, requestOptions: options);
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }
}
