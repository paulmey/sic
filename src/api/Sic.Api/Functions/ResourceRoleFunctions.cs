using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core;
using Sic.Core.Repositories;
using Sic.Core.Models;

namespace Sic.Api.Functions;

public class ResourceRoleFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IResourceRoleRepository _roleRepo;
    private readonly IUserRepository _userRepo;

    public ResourceRoleFunctions(IResourceRoleRepository roleRepo, IUserRepository userRepo)
    {
        _roleRepo = roleRepo;
        _userRepo = userRepo;
    }

    [Function("GetResourceRoles")]
    public async Task<IActionResult> GetResourceRoles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/{resourceId}/roles")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        var roles = await _roleRepo.GetByResourceAsync(resourceId);
        return new OkObjectResult(roles);
    }

    [Function("CreateResourceRole")]
    public async Task<IActionResult> CreateResourceRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources/{resourceId}/roles")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        var body = await JsonSerializer.DeserializeAsync<ResourceRoleRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.UserId) || string.IsNullOrWhiteSpace(body.Role))
            return new BadRequestObjectResult(new { error = "UserId and Role are required." });

        if (body.Role != ResourceRoles.User && body.Role != ResourceRoles.Manager)
            return new BadRequestObjectResult(new { error = $"Role must be '{ResourceRoles.User}' or '{ResourceRoles.Manager}'." });

        var existing = await _roleRepo.GetByResourceAndUserAsync(resourceId, body.UserId);
        if (existing is not null)
            return new ConflictObjectResult(new { error = "User already has a role on this resource." });

        var role = new ResourceRole
        {
            Id = Guid.NewGuid().ToString(),
            ResourceId = resourceId,
            UserId = body.UserId,
            Role = body.Role
        };

        await _roleRepo.CreateAsync(role);
        return new CreatedResult($"/api/resources/{resourceId}/roles/{role.Id}", role);
    }

    [Function("UpdateResourceRole")]
    public async Task<IActionResult> UpdateResourceRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "resources/{resourceId}/roles/{userId}")] HttpRequest req,
        string resourceId, string userId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        var existing = await _roleRepo.GetByResourceAndUserAsync(resourceId, userId);
        if (existing is null)
            return new NotFoundResult();

        var body = await JsonSerializer.DeserializeAsync<ResourceRoleRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.Role))
            return new BadRequestObjectResult(new { error = "Role is required." });

        if (body.Role != ResourceRoles.User && body.Role != ResourceRoles.Manager)
            return new BadRequestObjectResult(new { error = $"Role must be '{ResourceRoles.User}' or '{ResourceRoles.Manager}'." });

        existing.Role = body.Role;
        await _roleRepo.UpdateAsync(existing);
        return new OkObjectResult(existing);
    }

    [Function("DeleteResourceRole")]
    public async Task<IActionResult> DeleteResourceRole(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId}/roles/{userId}")] HttpRequest req,
        string resourceId, string userId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        await _roleRepo.DeleteAsync(resourceId, userId);
        return new NoContentResult();
    }
}

public record ResourceRoleRequest(string? UserId, string? Role);
