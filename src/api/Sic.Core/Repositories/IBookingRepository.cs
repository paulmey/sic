using Sic.Core.Models;

namespace Sic.Core.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(string resourceId, string bookingId);
    Task<IEnumerable<Booking>> GetByResourceAsync(string resourceId, DateTimeOffset from, DateTimeOffset to);
    Task<bool> HasOverlapAsync(string resourceId, DateTimeOffset startTime, DateTimeOffset endTime, string? excludeBookingId = null);
    Task CreateAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task DeleteAsync(string resourceId, string bookingId);
}
