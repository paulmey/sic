using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core;
using Sic.Core.Repositories;
using Sic.Core.Models;

namespace Sic.Api.Functions;

public class CategoryFunctions
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ICategoryRepository _categoryRepo;
    private readonly IUserRepository _userRepo;

    public CategoryFunctions(ICategoryRepository categoryRepo, IUserRepository userRepo)
    {
        _categoryRepo = categoryRepo;
        _userRepo = userRepo;
    }

    [Function("GetCategories")]
    public async Task<IActionResult> GetCategories(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "categories")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var categories = await _categoryRepo.GetAllAsync();
        return new OkObjectResult(categories);
    }

    [Function("CreateCategory")]
    public async Task<IActionResult> CreateCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "categories")] HttpRequest req)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.CategoryAdmin))
            return new StatusCodeResult(403);

        var body = await JsonSerializer.DeserializeAsync<CreateCategoryRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(new { error = "Name is required." });

        var category = new Category
        {
            Id = Guid.NewGuid().ToString(),
            Name = body.Name,
            Icon = body.Icon ?? "",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _categoryRepo.CreateAsync(category);
        return new CreatedResult($"/api/categories/{category.Id}", category);
    }

    [Function("UpdateCategory")]
    public async Task<IActionResult> UpdateCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "categories/{categoryId}")] HttpRequest req,
        string categoryId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.CategoryAdmin))
            return new StatusCodeResult(403);

        var existing = await _categoryRepo.GetByIdAsync(categoryId);
        if (existing is null)
            return new NotFoundResult();

        var body = await JsonSerializer.DeserializeAsync<CreateCategoryRequest>(req.Body, JsonOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.Name))
            return new BadRequestObjectResult(new { error = "Name is required." });

        existing.Name = body.Name;
        existing.Icon = body.Icon ?? existing.Icon;

        await _categoryRepo.UpdateAsync(existing);
        return new OkObjectResult(existing);
    }

    [Function("DeleteCategory")]
    public async Task<IActionResult> DeleteCategory(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "categories/{categoryId}")] HttpRequest req,
        string categoryId)
    {
        var principal = AuthHelper.GetClientPrincipal(req);
        if (principal is null)
            return new UnauthorizedResult();

        var user = await _userRepo.GetByIdentityAsync(principal.IdentityProvider, principal.UserId);
        if (user is null || !user.AppRoles.Contains(AppRoles.CategoryAdmin))
            return new StatusCodeResult(403);

        await _categoryRepo.DeleteAsync(categoryId);
        return new NoContentResult();
    }
}

public record CreateCategoryRequest(string? Name, string? Icon);
