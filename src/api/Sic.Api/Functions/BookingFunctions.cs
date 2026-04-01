using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Functions;

public class BookingFunctions
{
    private readonly BookingService _bookingService;
    private readonly IBookingRepository _bookingRepo;

    public BookingFunctions(BookingService bookingService, IBookingRepository bookingRepo)
    {
        _bookingService = bookingService;
        _bookingRepo = bookingRepo;
    }

    [Function("GetBookings")]
    public async Task<IActionResult> GetBookings(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/{resourceId}/bookings")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var fromParam = req.Query["from"].FirstOrDefault();
        var toParam = req.Query["to"].FirstOrDefault();

        if (!DateTimeOffset.TryParse(fromParam, out var from) || !DateTimeOffset.TryParse(toParam, out var to))
            return new BadRequestObjectResult(new { error = "Query parameters 'from' and 'to' are required (ISO 8601)." });

        var bookings = await _bookingRepo.GetByResourceAsync(resourceId, from, to);
        return new OkObjectResult(bookings);
    }

    [Function("CreateBooking")]
    public async Task<IActionResult> CreateBooking(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources/{resourceId}/bookings")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var body = await JsonSerializer.DeserializeAsync<CreateBookingRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (body is null)
            return new BadRequestObjectResult(new { error = "Invalid request body." });

        var result = await _bookingService.CreateBookingAsync(
            resourceId, principal.UserId, body.Title ?? "", body.Description ?? "",
            body.StartTime, body.EndTime);

        if (!result.Success)
            return new ConflictObjectResult(new { error = result.Error });

        return new CreatedResult($"/api/resources/{resourceId}/bookings/{result.Value!.Id}", result.Value);
    }

    [Function("DeleteBooking")]
    public async Task<IActionResult> DeleteBooking(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId}/bookings/{bookingId}")] HttpRequest req,
        string resourceId, string bookingId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var result = await _bookingService.DeleteBookingAsync(resourceId, bookingId, principal.UserId);

        if (!result.Success)
            return new BadRequestObjectResult(new { error = result.Error });

        return new NoContentResult();
    }
}

public record CreateBookingRequest(string? Title, string? Description, DateTimeOffset StartTime, DateTimeOffset EndTime);
