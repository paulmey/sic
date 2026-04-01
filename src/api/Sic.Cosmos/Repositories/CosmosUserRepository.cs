using Microsoft.Azure.Cosmos;
using Sic.Core.Repositories;
using Sic.Cosmos.Documents;
using User = Sic.Core.Models.User;

namespace Sic.Cosmos.Repositories;

public class CosmosUserRepository : CosmosRepository, IUserRepository
{
    public CosmosUserRepository(CosmosClient client, string databaseName, string containerName)
        : base(client, databaseName, containerName) { }

    public async Task<User?> GetByIdAsync(string id)
    {
        var doc = await ReadItemOrDefaultAsync<UserDocument>(id, $"user:{id}");
        return doc is null ? null : Mapper.ToModel(doc);
    }

    public async Task<User?> GetByIdentityAsync(string identityProvider, string identityId)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'user' AND c.identityProvider = @ip AND c.identityId = @iid")
            .WithParameter("@ip", identityProvider)
            .WithParameter("@iid", identityId);

        var docs = await QueryAsync<UserDocument>(query);
        return docs.Count > 0 ? Mapper.ToModel(docs[0]) : null;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'user'");
        var docs = await QueryAsync<UserDocument>(query);
        return docs.Select(Mapper.ToModel);
    }

    public async Task<int> CountAsync()
    {
        var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.type = 'user'");
        var results = await QueryAsync<int>(query);
        return results.FirstOrDefault();
    }

    public async Task CreateAsync(User user)
    {
        var doc = Mapper.ToDocument(user);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk));
    }

    public async Task UpdateAsync(User user)
    {
        var doc = Mapper.ToDocument(user);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Pk));
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<UserDocument>(id, new PartitionKey($"user:{id}"));
    }
}
