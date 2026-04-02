using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Api.Tests;

public class CategoryFunctionsTests
{
    private readonly ICategoryRepository _categoryRepo = Substitute.For<ICategoryRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly CategoryFunctions _sut;

    private readonly User _adminUser = new()
    {
        Id = "u1", IdentityProvider = "microsoft", IdentityId = "user-1",
        DisplayName = "Admin", AppRoles = new List<string> { AppRoles.CategoryAdmin }
    };

    private readonly User _regularUser = new()
    {
        Id = "u2", IdentityProvider = "microsoft", IdentityId = "user-2",
        DisplayName = "Regular", AppRoles = new List<string>()
    };

    public CategoryFunctionsTests()
    {
        _sut = new CategoryFunctions(_categoryRepo, _userRepo);
    }

    [Fact]
    public async Task CreateCategory_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.CreateCategory(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateCategory_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { name = "Test" });

        var result = await _sut.CreateCategory(req);

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithAdminRole_Returns201()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { name = "Meeting Rooms" });

        var result = await _sut.CreateCategory(req);

        Assert.IsType<CreatedResult>(result);
        await _categoryRepo.Received(1).CreateAsync(Arg.Is<Category>(c => c.Name == "Meeting Rooms"));
    }

    [Fact]
    public async Task DeleteCategory_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.DeleteCategory(req, "cat-1");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.DeleteCategory(req, "cat-1");

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
        await _categoryRepo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteCategory_WithAdminRole_Returns204()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest();

        var result = await _sut.DeleteCategory(req, "cat-1");

        Assert.IsType<NoContentResult>(result);
        await _categoryRepo.Received(1).DeleteAsync("cat-1");
    }

    [Fact]
    public async Task GetCategories_Authenticated_Returns200()
    {
        var categories = new List<Category> { new() { Id = "c1", Name = "Rooms" } };
        _categoryRepo.GetAllAsync().Returns(categories);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetCategories(req);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CreateCategory_MissingName_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { name = "" });

        var result = await _sut.CreateCategory(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- GetCategories ---

    [Fact]
    public async Task GetCategories_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.GetCategories(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    // --- UpdateCategory ---

    [Fact]
    public async Task UpdateCategory_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.UpdateCategory(req, "cat-1");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateCategory_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { name = "Updated" });

        var result = await _sut.UpdateCategory(req, "cat-1");

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_NotFound_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _categoryRepo.GetByIdAsync("cat-missing").Returns((Category?)null);
        var req = TestHelper.CreateRequest(body: new { name = "Updated" });

        var result = await _sut.UpdateCategory(req, "cat-missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateCategory_Valid_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _categoryRepo.GetByIdAsync("cat-1").Returns(new Category { Id = "cat-1", Name = "Old" });
        var req = TestHelper.CreateRequest(body: new { name = "New Name" });

        var result = await _sut.UpdateCategory(req, "cat-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        await _categoryRepo.Received(1).UpdateAsync(Arg.Is<Category>(c => c.Name == "New Name"));
    }

    [Fact]
    public async Task UpdateCategory_MissingName_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _categoryRepo.GetByIdAsync("cat-1").Returns(new Category { Id = "cat-1", Name = "Old" });
        var req = TestHelper.CreateRequest(body: new { name = "" });

        var result = await _sut.UpdateCategory(req, "cat-1");

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
