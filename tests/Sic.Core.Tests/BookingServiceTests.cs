using NSubstitute;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Core.Tests;

public class BookingServiceTests
{
    private readonly IBookingRepository _bookingRepo = Substitute.For<IBookingRepository>();
    private readonly IResourceRoleRepository _roleRepo = Substitute.For<IResourceRoleRepository>();
    private readonly BookingService _sut;

    private const string ResourceId = "resource-1";
    private const string UserId = "user-1";

    public BookingServiceTests()
    {
        _sut = new BookingService(_bookingRepo, _roleRepo);
    }

    [Fact]
    public async Task CreateBooking_WithValidData_Succeeds()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null)
            .Returns(false);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.CreateBookingAsync(ResourceId, UserId, "Team meeting", "", start, end);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(ResourceId, result.Value!.ResourceId);
        Assert.Equal(UserId, result.Value.UserId);
        await _bookingRepo.Received(1).CreateAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task CreateBooking_WithOverlap_Fails()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null)
            .Returns(true);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.CreateBookingAsync(ResourceId, UserId, "Team meeting", "", start, end);

        Assert.False(result.Success);
        Assert.Contains("overlap", result.Error!, StringComparison.OrdinalIgnoreCase);
        await _bookingRepo.DidNotReceive().CreateAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task CreateBooking_EndBeforeStart_Fails()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = start.AddHours(-1);

        var result = await _sut.CreateBookingAsync(ResourceId, UserId, "Bad booking", "", start, end);

        Assert.False(result.Success);
        Assert.Contains("end", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBooking_TitleTooLong_Fails()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);
        var longTitle = new string('x', 31);

        var result = await _sut.CreateBookingAsync(ResourceId, UserId, longTitle, "", start, end);

        Assert.False(result.Success);
        Assert.Contains("title", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateBooking_WithoutResourceRole_Fails()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns((ResourceRole?)null);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.CreateBookingAsync(ResourceId, UserId, "Meeting", "", start, end);

        Assert.False(result.Success);
        Assert.Contains("permission", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteBooking_ByOwner_Succeeds()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = UserId };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var result = await _sut.DeleteBookingAsync(ResourceId, "b1", UserId);

        Assert.True(result.Success);
        await _bookingRepo.Received(1).DeleteAsync(ResourceId, "b1");
    }

    [Fact]
    public async Task DeleteBooking_ByNonOwnerNonManager_Fails()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = "other-user" };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var result = await _sut.DeleteBookingAsync(ResourceId, "b1", UserId);

        Assert.False(result.Success);
        await _bookingRepo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteBooking_ByManager_Succeeds()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = "other-user" };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "manager" });

        var result = await _sut.DeleteBookingAsync(ResourceId, "b1", UserId);

        Assert.True(result.Success);
        await _bookingRepo.Received(1).DeleteAsync(ResourceId, "b1");
    }

    // --- CreateBooking edge cases ---

    [Fact]
    public async Task CreateBooking_DescriptionTooLong_Fails()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);
        var longDescription = new string('x', 1001);

        var result = await _sut.CreateBookingAsync(ResourceId, UserId, "OK title", longDescription, start, end);

        Assert.False(result.Success);
        Assert.Contains("description", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // --- DeleteBooking edge cases ---

    [Fact]
    public async Task DeleteBooking_NotFound_Fails()
    {
        _bookingRepo.GetByIdAsync(ResourceId, "b-missing").Returns((Booking?)null);

        var result = await _sut.DeleteBookingAsync(ResourceId, "b-missing", UserId);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // --- UpdateBooking ---

    [Fact]
    public async Task UpdateBooking_Valid_Succeeds()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = UserId };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), "b1")
            .Returns(false);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, "Updated", "desc", start, end);

        Assert.True(result.Success);
        Assert.Equal("Updated", result.Value!.Title);
        await _bookingRepo.Received(1).UpdateAsync(Arg.Any<Booking>());
    }

    [Fact]
    public async Task UpdateBooking_NotFound_Fails()
    {
        _bookingRepo.GetByIdAsync(ResourceId, "b-missing").Returns((Booking?)null);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b-missing", UserId, "T", "d", start, end);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBooking_NoPermission_Fails()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = UserId };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId).Returns((ResourceRole?)null);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, "T", "d", start, end);

        Assert.False(result.Success);
        Assert.Contains("permission", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBooking_NotOwnerNotManager_Fails()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = "other-user" };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, "T", "d", start, end);

        Assert.False(result.Success);
        Assert.Contains("owner", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBooking_ByManager_Succeeds()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = "other-user" };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "manager" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), "b1")
            .Returns(false);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, "Updated", "d", start, end);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateBooking_EndBeforeStart_Fails()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = UserId };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = start.AddHours(-1);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, "T", "d", start, end);

        Assert.False(result.Success);
        Assert.Contains("end", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBooking_TitleTooLong_Fails()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = UserId };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);
        var longTitle = new string('x', 31);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, longTitle, "d", start, end);

        Assert.False(result.Success);
        Assert.Contains("title", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateBooking_Overlap_Fails()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = UserId };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, UserId)
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = UserId, Role = "user" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), "b1")
            .Returns(true);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);

        var result = await _sut.UpdateBookingAsync(ResourceId, "b1", UserId, "T", "d", start, end);

        Assert.False(result.Success);
        Assert.Contains("overlap", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
