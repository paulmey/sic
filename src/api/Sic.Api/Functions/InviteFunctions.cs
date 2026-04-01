using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Functions;

public class InviteFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly InviteService _inviteService;
    private readonly UserService _userService;
    private readonly IInviteLinkRepository _inviteRepo;
    private readonly IUserRepository _userRepo;

    public InviteFunctions(InviteService inviteService, UserService userService,
        IInviteLinkRepository inviteRepo, IUserRepository userRepo)
    {
        _inviteService = inviteService;
        _userService = userService;
        _inviteRepo = inviteRepo;
        _userRepo = userRepo;
    }

    [Function("CreateInvite")]
    public async Task<IActionResult> CreateInvite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invites")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.UserAdmin))
            return new StatusCodeResult(403);

        var body = await JsonSerializer.DeserializeAsync<CreateInviteRequest>(req.Body, JsonOptions);

        var validityDays = body?.ValidityDays ?? 7;
        var result = await _inviteService.CreateInviteAsync(principal.UserId, TimeSpan.FromDays(validityDays));

        if (!result.Success)
            return new BadRequestObjectResult(new { error = result.Error });

        return new CreatedResult($"/api/invites/{result.Value!.Id}", result.Value);
    }

    [Function("RedeemInvite")]
    public async Task<IActionResult> RedeemInvite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "invite/redeem")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var body = await JsonSerializer.DeserializeAsync<RedeemInviteRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.InviteId))
            return new BadRequestObjectResult(new { error = "InviteId is required." });

        var result = await _userService.AuthenticateOrCreateWithInviteAsync(
            principal.IdentityProvider, principal.UserId, principal.UserDetails, body.InviteId);

        if (!result.Success)
            return new BadRequestObjectResult(new { error = result.Error });

        return new OkObjectResult(result.Value);
    }

    [Function("GetInvites")]
    public async Task<IActionResult> GetInvites(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "invites")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.UserAdmin))
            return new StatusCodeResult(403);

        var invites = await _inviteRepo.GetActiveAsync();
        return new OkObjectResult(invites);
    }

    [Function("DeleteInvite")]
    public async Task<IActionResult> DeleteInvite(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "invites/{inviteId}")] HttpRequest req,
        string inviteId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.UserAdmin))
            return new StatusCodeResult(403);

        await _inviteRepo.DeleteAsync(inviteId);

        return new NoContentResult();
    }
}

public record CreateInviteRequest(int? ValidityDays);
public record RedeemInviteRequest(string? InviteId);
