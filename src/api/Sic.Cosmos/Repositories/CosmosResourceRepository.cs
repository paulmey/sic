using Microsoft.Azure.Cosmos;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Cosmos.Documents;

namespace Sic.Cosmos.Repositories;

public class CosmosResourceRepository : CosmosRepository, IResourceRepository
{
    protected override string TypeName => "resource";

    public CosmosResourceRepository(CosmosClient client, string databaseName, string containerName)
        : base(client, databaseName, containerName) { }

    public async Task<Resource?> GetByIdAsync(string id)
    {
        var doc = await ReadItemOrDefaultAsync<ResourceDocument>(id, $"resource:{id}");
        return doc is null ? null : Mapper.ToModel(doc);
    }

    public async Task<IEnumerable<Resource>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'resource'");
        var docs = await QueryAsync<ResourceDocument>(query);
        return docs.Select(Mapper.ToModel);
    }

    public async Task<IEnumerable<Resource>> GetByCategoryAsync(string categoryId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'resource' AND c.categoryId = @cid")
            .WithParameter("@cid", categoryId);
        var docs = await QueryAsync<ResourceDocument>(query);
        return docs.Select(Mapper.ToModel);
    }

    public async Task CreateAsync(Resource resource)
    {
        var doc = Mapper.ToDocument(resource);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk));
    }

    public async Task UpdateAsync(Resource resource)
    {
        var doc = Mapper.ToDocument(resource);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Pk));
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<ResourceDocument>(id, new PartitionKey($"resource:{id}"));
    }
}
