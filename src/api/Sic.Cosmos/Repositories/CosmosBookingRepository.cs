using Microsoft.Azure.Cosmos;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Cosmos.Documents;

namespace Sic.Cosmos.Repositories;

public class CosmosBookingRepository : CosmosRepository, IBookingRepository
{
    public CosmosBookingRepository(CosmosClient client, string databaseName, string containerName)
        : base(client, databaseName, containerName) { }

    public async Task<Booking?> GetByIdAsync(string resourceId, string bookingId)
    {
        var doc = await ReadItemOrDefaultAsync<BookingDocument>(bookingId, $"resource:{resourceId}");
        return doc is null ? null : Mapper.ToModel(doc);
    }

    public async Task<IEnumerable<Booking>> GetByResourceAsync(
        string resourceId, DateTimeOffset from, DateTimeOffset to)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.type = 'booking' AND c.resourceId = @rid " +
            "AND c.endTime >= @from AND c.startTime <= @to ORDER BY c.startTime")
            .WithParameter("@rid", resourceId)
            .WithParameter("@from", from)
            .WithParameter("@to", to);
        var docs = await QueryAsync<BookingDocument>(query, $"resource:{resourceId}");
        return docs.Select(Mapper.ToModel);
    }

    public async Task<bool> HasOverlapAsync(
        string resourceId, DateTimeOffset startTime, DateTimeOffset endTime, string? excludeBookingId = null)
    {
        var sql = "SELECT VALUE COUNT(1) FROM c WHERE c.type = 'booking' AND c.resourceId = @rid " +
                  "AND c.startTime < @end AND c.endTime > @start";
        if (excludeBookingId is not null)
            sql += " AND c.id != @excludeId";

        var query = new QueryDefinition(sql)
            .WithParameter("@rid", resourceId)
            .WithParameter("@start", startTime)
            .WithParameter("@end", endTime);

        if (excludeBookingId is not null)
            query = query.WithParameter("@excludeId", excludeBookingId);

        var results = await QueryAsync<int>(query, $"resource:{resourceId}");
        return results.FirstOrDefault() > 0;
    }

    public async Task CreateAsync(Booking booking)
    {
        var doc = Mapper.ToDocument(booking);
        await _container.CreateItemAsync(doc, new PartitionKey(doc.Pk));
    }

    public async Task UpdateAsync(Booking booking)
    {
        var doc = Mapper.ToDocument(booking);
        await _container.ReplaceItemAsync(doc, doc.Id, new PartitionKey(doc.Pk));
    }

    public async Task DeleteAsync(string resourceId, string bookingId)
    {
        await _container.DeleteItemAsync<BookingDocument>(
            bookingId, new PartitionKey($"resource:{resourceId}"));
    }
}
