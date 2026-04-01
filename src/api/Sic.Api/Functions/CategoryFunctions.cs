using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Sic.Core.Repositories;
using Sic.Core.Models;

namespace Sic.Api.Functions;

public class CategoryFunctions
{
    private readonly ICategoryRepository _categoryRepo;

    public CategoryFunctions(ICategoryRepository categoryRepo)
    {
        _categoryRepo = categoryRepo;
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

        // TODO: Check category-admin role

        var body = await JsonSerializer.DeserializeAsync<CreateCategoryRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        // TODO: Check category-admin role

        var existing = await _categoryRepo.GetByIdAsync(categoryId);
        if (existing is null)
            return new NotFoundResult();

        var body = await JsonSerializer.DeserializeAsync<CreateCategoryRequest>(req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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

        // TODO: Check category-admin role

        await _categoryRepo.DeleteAsync(categoryId);
        return new NoContentResult();
    }
}

public record CreateCategoryRequest(string? Name, string? Icon);
