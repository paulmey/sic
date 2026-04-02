using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core;
using Sic.Core.Repositories;
using Sic.Core.Models;

namespace Sic.Api.Functions;

public class ResourceFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IResourceRepository _resourceRepo;
    private readonly IResourceRoleRepository _roleRepo;
    private readonly IUserRepository _userRepo;

    public ResourceFunctions(IResourceRepository resourceRepo, IResourceRoleRepository roleRepo, IUserRepository userRepo)
    {
        _resourceRepo = resourceRepo;
        _roleRepo = roleRepo;
        _userRepo = userRepo;
    }

    [Function("GetResources")]
    public async Task<IActionResult> GetResources(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null)
            return new UnauthorizedResult();

        var categoryId = req.Query["categoryId"].FirstOrDefault();
        var resources = string.IsNullOrEmpty(categoryId)
            ? await _resourceRepo.GetAllAsync()
            : await _resourceRepo.GetByCategoryAsync(categoryId);

        // Resource admins see all resources; regular users only see resources they have roles on
        if (!user.AppRoles.Contains(AppRoles.ResourceAdmin))
        {
            var roles = await _roleRepo.GetByUserAsync(user.Id);
            var allowedResourceIds = new HashSet<string>(roles.Select(r => r.ResourceId));
            resources = resources.Where(r => allowedResourceIds.Contains(r.Id));
        }

        return new OkObjectResult(resources);
    }

    [Function("GetResource")]
    public async Task<IActionResult> GetResource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "resources/{resourceId}")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null)
            return new UnauthorizedResult();

        var resource = await _resourceRepo.GetByIdAsync(resourceId);
        if (resource is null)
            return new NotFoundResult();

        if (!user.AppRoles.Contains(AppRoles.ResourceAdmin))
        {
            var role = await _roleRepo.GetByResourceAndUserAsync(resourceId, user.Id);
            if (role is null)
                return new NotFoundResult();
        }

        return new OkObjectResult(resource);
    }

    [Function("CreateResource")]
    public async Task<IActionResult> CreateResource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "resources")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        var body = await JsonSerializer.DeserializeAsync<CreateResourceRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(new { error = "Name is required." });

        var resource = new Resource
        {
            Id = Guid.NewGuid().ToString(),
            CategoryId = body.CategoryId ?? "",
            Name = body.Name,
            Description = body.Description ?? "",
            ImageUrl = body.ImageUrl ?? "",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _resourceRepo.CreateAsync(resource);
        return new CreatedResult($"/api/resources/{resource.Id}", resource);
    }

    [Function("UpdateResource")]
    public async Task<IActionResult> UpdateResource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "resources/{resourceId}")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        var existing = await _resourceRepo.GetByIdAsync(resourceId);
        if (existing is null)
            return new NotFoundResult();

        var body = await JsonSerializer.DeserializeAsync<CreateResourceRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(new { error = "Name is required." });

        existing.Name = body.Name;
        existing.CategoryId = body.CategoryId ?? existing.CategoryId;
        existing.Description = body.Description ?? existing.Description;
        existing.ImageUrl = body.ImageUrl ?? existing.ImageUrl;

        await _resourceRepo.UpdateAsync(existing);
        return new OkObjectResult(existing);
    }

    [Function("DeleteResource")]
    public async Task<IActionResult> DeleteResource(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "resources/{resourceId}")] HttpRequest req,
        string resourceId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.ResourceAdmin))
            return new StatusCodeResult(403);

        await _resourceRepo.DeleteAsync(resourceId);
        return new NoContentResult();
    }
}

public record CreateResourceRequest(string? Name, string? CategoryId, string? Description, string? ImageUrl);
