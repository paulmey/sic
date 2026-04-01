using Microsoft.Azure.Cosmos;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Cosmos.Documents;

namespace Sic.Cosmos.Repositories;

public class CosmosCategoryRepository : CosmosRepository, ICategoryRepository
{
    protected override string TypeName => "category";

    public CosmosCategoryRepository(CosmosClient client, string databaseName, string containerName)
        : base(client, databaseName, containerName) { }

    public async Task<Category?> GetByIdAsync(string id)
    {
        var doc = await ReadItemOrDefaultAsync<CategoryDocument>(id, $"category:{id}");
        return doc is null ? null : Mapper.ToModel(doc);
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.type = 'category'");
        var docs = await QueryAsync<CategoryDocument>(query);
        return docs.Select(Mapper.ToModel);
    }

    public async Task CreateAsync(Category category)
    {
        var doc = Mapper.ToDocument(category);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk));
    }

    public async Task UpdateAsync(Category category)
    {
        var doc = Mapper.ToDocument(category);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Pk));
    }

    public async Task DeleteAsync(string id)
    {
        await _container.DeleteItemAsync<CategoryDocument>(id, new PartitionKey($"category:{id}"));
    }
}
