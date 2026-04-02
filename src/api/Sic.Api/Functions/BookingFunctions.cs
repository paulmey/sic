using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Functions;

public class BookingFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly BookingService _bookingService;
    private readonly IBookingRepository _bookingRepo;
    private readonly IUserRepository _userRepo;

    public BookingFunctions(BookingService bookingService, IBookingRepository bookingRepo, IUserRepository userRepo)
    {
        _bookingService = bookingService;
        _bookingRepo = bookingRepo;
        _userRepo = userRepo;
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

        var userIds = bookings.Select(b => b.UserId).Distinct().ToList();
        var userNames = new Dictionary<string, string>();
        foreach (var uid in userIds)
        {
            var u = await _userRepo.GetByIdAsync(uid);
            if (u is not null) userNames[uid] = u.DisplayName;
        }

        var result = bookings.Select(b => new
        {
            b.Id, b.ResourceId, b.UserId, b.Title, b.Description,
            b.StartTime, b.EndTime, b.CreatedAt,
            UserDisplayName = userNames.GetValueOrDefault(b.UserId, "Unknown")
        });

        return new OkObjectResult(result);
    }

    [Function("CreateBooking")]
    public async Task<IActionResult> CreateBooking(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources/{resourceId}/bookings")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null)
            return new UnauthorizedResult();

        var body = await JsonSerializer.DeserializeAsync<CreateBookingRequest>(req.Body, JsonOptions);
        if (body is null)
            return new BadRequestObjectResult(new { error = "Invalid request body." });

        var result = await _bookingService.CreateBookingAsync(
            resourceId, user.Id, body.Title ?? "", body.Description ?? "",
            body.StartTime, body.EndTime);

        if (!result.Success)
        {
            if (result.Error!.Contains("overlap", StringComparison.OrdinalIgnoreCase))
                return new ConflictObjectResult(new { error = result.Error });
            return new BadRequestObjectResult(new { error = result.Error });
        }

        return new CreatedResult($"/api/resources/{resourceId}/bookings/{result.Value!.Id}", result.Value);
    }

    [Function("UpdateBooking")]
    public async Task<IActionResult> UpdateBooking(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "resources/{resourceId}/bookings/{bookingId}")] HttpRequest req,
        string resourceId, string bookingId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null)
            return new UnauthorizedResult();

        var body = await JsonSerializer.DeserializeAsync<CreateBookingRequest>(req.Body, JsonOptions);
        if (body is null)
            return new BadRequestObjectResult(new { error = "Invalid request body." });

        var result = await _bookingService.UpdateBookingAsync(
            resourceId, bookingId, user.Id, body.Title ?? "", body.Description ?? "",
            body.StartTime, body.EndTime);

        if (!result.Success)
        {
            if (result.Error!.Contains("overlap", StringComparison.OrdinalIgnoreCase))
                return new ConflictObjectResult(new { error = result.Error });
            return new BadRequestObjectResult(new { error = result.Error });
        }

        return new OkObjectResult(result.Value);
    }

    [Function("DeleteBooking")]
    public async Task<IActionResult> DeleteBooking(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId}/bookings/{bookingId}")] HttpRequest req,
        string resourceId, string bookingId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null)
            return new UnauthorizedResult();

        var result = await _bookingService.DeleteBookingAsync(resourceId, bookingId, user.Id);

        if (!result.Success)
            return new BadRequestObjectResult(new { error = result.Error });

        return new NoContentResult();
    }
}

public record CreateBookingRequest(string? Title, string? Description, DateTimeOffset StartTime, DateTimeOffset EndTime);
