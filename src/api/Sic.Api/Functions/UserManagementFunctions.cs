using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core;
using Sic.Core.Repositories;
using Sic.Core.Models;

namespace Sic.Api.Functions;

public class UserManagementFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IUserRepository _userRepo;

    public UserManagementFunctions(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    [Function("GetUsers")]
    public async Task<IActionResult> GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "users")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.UserAdmin))
            return new StatusCodeResult(403);

        var users = await _userRepo.GetAllAsync();
        return new OkObjectResult(users);
    }

    [Function("UpdateUser")]
    public async Task<IActionResult> UpdateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "users/{userId}")] HttpRequest req,
        string userId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var callingUser = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (callingUser is null || !callingUser.AppRoles.Contains(AppRoles.UserAdmin))
            return new StatusCodeResult(403);

        var targetUser = await _userRepo.GetByIdAsync(userId);
        if (targetUser is null)
            return new NotFoundResult();

        var body = await JsonSerializer.DeserializeAsync<UpdateUserRequest>(req.Body, JsonOptions);
        if (body is null)
            return new BadRequestObjectResult(new { error = "Request body is required." });

        // Validate roles
        if (body.AppRoles is not null)
        {
            foreach (var role in body.AppRoles)
            {
                if (!AppRoles.All.Contains(role))
                    return new BadRequestObjectResult(new { error = $"Invalid role: {role}" });
            }
            targetUser.AppRoles = body.AppRoles;
        }

        if (!string.IsNullOrWhiteSpace(body.DisplayName))
            targetUser.DisplayName = body.DisplayName;

        await _userRepo.UpdateAsync(targetUser);
        return new OkObjectResult(targetUser);
    }

    [Function("DeleteUser")]
    public async Task<IActionResult> DeleteUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "users/{userId}")] HttpRequest req,
        string userId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var callingUser = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (callingUser is null || !callingUser.AppRoles.Contains(AppRoles.UserAdmin))
            return new StatusCodeResult(403);

        // Prevent self-deletion
        if (callingUser.Id == userId)
            return new BadRequestObjectResult(new { error = "Cannot delete yourself." });

        await _userRepo.DeleteAsync(userId);
        return new NoContentResult();
    }
}

public record UpdateUserRequest(string? DisplayName, List<string>? AppRoles);
