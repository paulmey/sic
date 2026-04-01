using Microsoft.Azure.Cosmos;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Cosmos.Documents;

namespace Sic.Cosmos.Repositories;

public class CosmosResourceRoleRepository : CosmosRepository, IResourceRoleRepository
{
    protected override string TypeName => "resource-role";

    public CosmosResourceRoleRepository(CosmosClient client, string databaseName, string containerName)
        : base(client, databaseName, containerName) { }

    public async Task<IEnumerable<ResourceRole>> GetByResourceAsync(string resourceId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'resource-role' AND c.resourceId = @rid")
            .WithParameter("@rid", resourceId);
        var docs = await QueryAsync<ResourceRoleDocument>(query, $"resource:{resourceId}");
        return docs.Select(Mapper.ToModel);
    }

    public async Task<ResourceRole?> GetByResourceAndUserAsync(string resourceId, string userId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'resource-role' AND c.resourceId = @rid AND c.userId = @uid")
            .WithParameter("@rid", resourceId)
            .WithParameter("@uid", userId);
        var docs = await QueryAsync<ResourceRoleDocument>(query, $"resource:{resourceId}");
        return docs.Count > 0 ? Mapper.ToModel(docs[0]) : null;
    }

    public async Task CreateAsync(ResourceRole role)
    {
        var doc = Mapper.ToDocument(role);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk));
    }

    public async Task DeleteAsync(string resourceId, string userId)
    {
        // Find the document first to get the id
        var role = await GetByResourceAndUserAsync(resourceId, userId);
        if (role is not null)
        {
            await _container.DeleteItemAsync<ResourceRoleDocument>(
                role.Id, new PartitionKey($"resource:{resourceId}"));
        }
    }
}
