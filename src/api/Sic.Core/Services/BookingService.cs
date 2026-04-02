using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Core.Services;

public class BookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IResourceRoleRepository _roleRepo;

    public BookingService(IBookingRepository bookingRepo, IResourceRoleRepository roleRepo)
    {
        _bookingRepo = bookingRepo;
        _roleRepo = roleRepo;
    }

    public async Task<ServiceResult<Booking>> CreateBookingAsync(
        string resourceId, string userId, string title, string description,
        DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var role = await _roleRepo.GetByResourceAndUserAsync(resourceId, userId);
        if (role is null)
            return ServiceResult<Booking>.Fail("No permission to book this resource.");

        if (endTime <= startTime)
            return ServiceResult<Booking>.Fail("End time must be after start time.");

        if (title.Length > 30)
            return ServiceResult<Booking>.Fail("Title must be 30 characters or less.");

        if (description.Length > 1000)
            return ServiceResult<Booking>.Fail("Description must be 1000 characters or less.");

        var hasOverlap = await _bookingRepo.HasOverlapAsync(resourceId, startTime, endTime);
        if (hasOverlap)
            return ServiceResult<Booking>.Fail("Booking overlaps with an existing reservation.");

        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            ResourceId = resourceId,
            UserId = userId,
            Title = title,
            Description = description,
            StartTime = startTime,
            EndTime = endTime,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _bookingRepo.CreateAsync(booking);
        return ServiceResult<Booking>.Ok(booking);
    }

    public async Task<ServiceResult<Booking>> UpdateBookingAsync(
        string resourceId, string bookingId, string userId,
        string title, string description, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        var booking = await _bookingRepo.GetByIdAsync(resourceId, bookingId);
        if (booking is null)
            return ServiceResult<Booking>.Fail("Booking not found.");

        if (booking.EndTime <= DateTimeOffset.UtcNow)
            return ServiceResult<Booking>.Fail("Past bookings cannot be modified.");

        var role = await _roleRepo.GetByResourceAndUserAsync(resourceId, userId);
        if (role is null)
            return ServiceResult<Booking>.Fail("No permission for this resource.");

        if (booking.UserId != userId && role.Role != ResourceRoles.Manager)
            return ServiceResult<Booking>.Fail("Only the owner or a manager can update this booking.");

        if (endTime <= startTime)
            return ServiceResult<Booking>.Fail("End time must be after start time.");

        if (title.Length > 30)
            return ServiceResult<Booking>.Fail("Title must be 30 characters or less.");

        if (description.Length > 1000)
            return ServiceResult<Booking>.Fail("Description must be 1000 characters or less.");

        var hasOverlap = await _bookingRepo.HasOverlapAsync(resourceId, startTime, endTime, bookingId);
        if (hasOverlap)
            return ServiceResult<Booking>.Fail("Booking overlaps with an existing reservation.");

        booking.Title = title;
        booking.Description = description;
        booking.StartTime = startTime;
        booking.EndTime = endTime;

        await _bookingRepo.UpdateAsync(booking);
        return ServiceResult<Booking>.Ok(booking);
    }

    public async Task<ServiceResult> DeleteBookingAsync(string resourceId, string bookingId, string userId)
    {
        var booking = await _bookingRepo.GetByIdAsync(resourceId, bookingId);
        if (booking is null)
            return ServiceResult.Fail("Booking not found.");

        if (booking.EndTime <= DateTimeOffset.UtcNow)
            return ServiceResult.Fail("Past bookings cannot be deleted.");

        var role = await _roleRepo.GetByResourceAndUserAsync(resourceId, userId);
        if (role is null)
            return ServiceResult.Fail("No permission for this resource.");

        if (booking.UserId != userId && role.Role != ResourceRoles.Manager)
            return ServiceResult.Fail("Only the owner or a manager can delete this booking.");

        await _bookingRepo.DeleteAsync(resourceId, bookingId);
        return ServiceResult.Ok();
    }
}
