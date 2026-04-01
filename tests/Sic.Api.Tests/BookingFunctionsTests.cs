using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Tests;

public class BookingFunctionsTests
{
    private readonly IBookingRepository _bookingRepo = Substitute.For<IBookingRepository>();
    private readonly IResourceRoleRepository _roleRepo = Substitute.For<IResourceRoleRepository>();
    private readonly BookingService _bookingService;
    private readonly BookingFunctions _sut;

    private const string ResourceId = "resource-1";

    public BookingFunctionsTests()
    {
        _bookingService = new BookingService(_bookingRepo, _roleRepo);
        _sut = new BookingFunctions(_bookingService, _bookingRepo);
    }

    [Fact]
    public async Task CreateBooking_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.CreateBooking(req, ResourceId);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateBooking_ValidationError_Returns400()
    {
        // end before start → validation error → should be 400, not 409
        _roleRepo.GetByResourceAndUserAsync(ResourceId, "user-1")
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = "user-1", Role = "user" });

        var start = DateTimeOffset.UtcNow.AddHours(2);
        var end = start.AddHours(-1);
        var req = TestHelper.CreateRequest(body: new
        {
            title = "Test",
            description = "",
            startTime = start,
            endTime = end
        });

        var result = await _sut.CreateBooking(req, ResourceId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateBooking_OverlapError_Returns409()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, "user-1")
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = "user-1", Role = "user" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null)
            .Returns(true);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);
        var req = TestHelper.CreateRequest(body: new
        {
            title = "Test",
            description = "",
            startTime = start,
            endTime = end
        });

        var result = await _sut.CreateBooking(req, ResourceId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateBooking_ValidData_Returns201()
    {
        _roleRepo.GetByResourceAndUserAsync(ResourceId, "user-1")
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = "user-1", Role = "user" });
        _bookingRepo.HasOverlapAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), null)
            .Returns(false);

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(2);
        var req = TestHelper.CreateRequest(body: new
        {
            title = "Meeting",
            description = "",
            startTime = start,
            endTime = end
        });

        var result = await _sut.CreateBooking(req, ResourceId);

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task GetBookings_MissingQueryParams_Returns400()
    {
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetBookings(req, ResourceId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBookings_ValidParams_Returns200()
    {
        var bookings = new List<Booking>();
        var from = DateTimeOffset.UtcNow;
        var to = from.AddDays(7);
        _bookingRepo.GetByResourceAsync(ResourceId, Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
            .Returns(bookings);

        var req = TestHelper.CreateRequest(queryParams: new Dictionary<string, string>
        {
            ["from"] = from.ToString("O"),
            ["to"] = to.ToString("O")
        });

        var result = await _sut.GetBookings(req, ResourceId);

        Assert.IsType<OkObjectResult>(result);
    }

    // --- DeleteBooking ---

    [Fact]
    public async Task DeleteBooking_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.DeleteBooking(req, ResourceId, "b1");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeleteBooking_Valid_Returns204()
    {
        var booking = new Booking { Id = "b1", ResourceId = ResourceId, UserId = "user-1" };
        _bookingRepo.GetByIdAsync(ResourceId, "b1").Returns(booking);
        _roleRepo.GetByResourceAndUserAsync(ResourceId, "user-1")
            .Returns(new ResourceRole { ResourceId = ResourceId, UserId = "user-1", Role = "user" });
        var req = TestHelper.CreateRequest();

        var result = await _sut.DeleteBooking(req, ResourceId, "b1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteBooking_NotFound_Returns400()
    {
        _bookingRepo.GetByIdAsync(ResourceId, "b-missing").Returns((Booking?)null);
        var req = TestHelper.CreateRequest();

        var result = await _sut.DeleteBooking(req, ResourceId, "b-missing");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateBooking_MissingRequiredFields_Returns400()
    {
        var req = TestHelper.CreateRequest(body: new { });

        var result = await _sut.CreateBooking(req, ResourceId);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetBookings_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.GetBookings(req, ResourceId);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
