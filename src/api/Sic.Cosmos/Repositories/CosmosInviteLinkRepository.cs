using Microsoft.Azure.Cosmos;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Cosmos.Documents;

namespace Sic.Cosmos.Repositories;

public class CosmosInviteLinkRepository : CosmosRepository, IInviteLinkRepository
{
    protected override string TypeName => "invite";

    public CosmosInviteLinkRepository(CosmosClient client, string databaseName, string containerName)
        : base(client, databaseName, containerName) { }

    public async Task<InviteLink?> GetByIdAsync(string id)
    {
        var doc = await ReadItemOrDefaultAsync<InviteLinkDocument>(id, $"invite:{id}");
        return doc is null ? null : Mapper.ToModel(doc);
    }

    public async Task<IEnumerable<InviteLink>> GetActiveAsync()
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'invite' AND c.usedByUserId = null AND c.expiresAt > @now")
            .WithParameter("@now", DateTimeOffset.UtcNow);
        var docs = await QueryAsync<InviteLinkDocument>(query);
        return docs.Select(Mapper.ToModel);
    }

    public async Task CreateAsync(InviteLink invite)
    {
        var doc = Mapper.ToDocument(invite);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk));
    }

    public async Task UpdateAsync(InviteLink invite)
    {
        var doc = Mapper.ToDocument(invite);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Pk));
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<InviteLinkDocument>(id, new PartitionKey($"invite:{id}"));
    }
}
