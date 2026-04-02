using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Api.Tests;

public class ResourceFunctionsTests
{
    private readonly IResourceRepository _resourceRepo = Substitute.For<IResourceRepository>();
    private readonly IResourceRoleRepository _roleRepo = Substitute.For<IResourceRoleRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ResourceFunctions _sut;

    private readonly User _adminUser = new()
    {
        Id = "u1", IdentityProvider = "microsoft", IdentityId = "user-1",
        DisplayName = "Admin", AppRoles = new List<string> { AppRoles.ResourceAdmin }
    };

    private readonly User _regularUser = new()
    {
        Id = "u2", IdentityProvider = "microsoft", IdentityId = "user-2",
        DisplayName = "Regular", AppRoles = new List<string>()
    };

    public ResourceFunctionsTests()
    {
        _sut = new ResourceFunctions(_resourceRepo, _roleRepo, _userRepo);
    }

    // --- GetResources ---

    [Fact]
    public async Task GetResources_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.GetResources(req);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetResources_Authenticated_ResourceAdmin_ReturnsAll()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var resources = new List<Resource> { new() { Id = "r1", Name = "Room A" }, new() { Id = "r2", Name = "Room B" } };
        _resourceRepo.GetAllAsync().Returns(resources);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetResources(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<Resource>>(ok.Value);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetResources_RegularUser_ReturnsOnlyAssignedResources()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var resources = new List<Resource>
        {
            new() { Id = "r1", Name = "Room A" },
            new() { Id = "r2", Name = "Room B" },
            new() { Id = "r3", Name = "Room C" }
        };
        _resourceRepo.GetAllAsync().Returns(resources);
        _roleRepo.GetByUserAsync("u2").Returns(new List<ResourceRole>
        {
            new() { Id = "rr1", ResourceId = "r1", UserId = "u2", Role = "user" },
            new() { Id = "rr3", ResourceId = "r3", UserId = "u2", Role = "manager" }
        });
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.GetResources(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<Resource>>(ok.Value).ToList();
        Assert.Equal(2, items.Count);
        Assert.Contains(items, r => r.Id == "r1");
        Assert.Contains(items, r => r.Id == "r3");
    }

    [Fact]
    public async Task GetResources_RegularUser_NoRoles_ReturnsEmpty()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        _resourceRepo.GetAllAsync().Returns(new List<Resource> { new() { Id = "r1", Name = "Room A" } });
        _roleRepo.GetByUserAsync("u2").Returns(new List<ResourceRole>());
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.GetResources(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<Resource>>(ok.Value);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetResources_UnknownUser_Returns401()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns((User?)null);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetResources(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetResources_WithCategoryFilter_FiltersByCategory()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var resources = new List<Resource> { new() { Id = "r1", Name = "Room A", CategoryId = "cat-1" } };
        _resourceRepo.GetByCategoryAsync("cat-1").Returns(resources);
        var req = TestHelper.CreateRequest(queryParams: new Dictionary<string, string> { ["categoryId"] = "cat-1" });

        var result = await _sut.GetResources(req);

        Assert.IsType<OkObjectResult>(result);
        await _resourceRepo.Received(1).GetByCategoryAsync("cat-1");
        await _resourceRepo.DidNotReceive().GetAllAsync();
    }

    // --- GetResource ---

    [Fact]
    public async Task GetResource_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.GetResource(req, "r1");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetResource_NotFound_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _resourceRepo.GetByIdAsync("r-missing").Returns((Resource?)null);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetResource(req, "r-missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetResource_ResourceAdmin_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _resourceRepo.GetByIdAsync("r1").Returns(new Resource { Id = "r1", Name = "Room A" });
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetResource(req, "r1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResource_UserWithRole_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        _resourceRepo.GetByIdAsync("r1").Returns(new Resource { Id = "r1", Name = "Room A" });
        _roleRepo.GetByResourceAndUserAsync("r1", "u2").Returns(new ResourceRole { Id = "rr1", ResourceId = "r1", UserId = "u2", Role = "user" });
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.GetResource(req, "r1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetResource_UserWithoutRole_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        _resourceRepo.GetByIdAsync("r1").Returns(new Resource { Id = "r1", Name = "Room A" });
        _roleRepo.GetByResourceAndUserAsync("r1", "u2").Returns((ResourceRole?)null);
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.GetResource(req, "r1");

        Assert.IsType<NotFoundResult>(result);
    }

    // --- CreateResource ---

    [Fact]
    public async Task CreateResource_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.CreateResource(req);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateResource_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { name = "Room" });

        var result = await _sut.CreateResource(req);

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task CreateResource_WithAdminRole_Returns201()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { name = "Conference Room", categoryId = "cat-1" });

        var result = await _sut.CreateResource(req);

        Assert.IsType<CreatedResult>(result);
        await _resourceRepo.Received(1).CreateAsync(Arg.Is<Resource>(r => r.Name == "Conference Room"));
    }

    [Fact]
    public async Task CreateResource_MissingName_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { name = "" });

        var result = await _sut.CreateResource(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- UpdateResource ---

    [Fact]
    public async Task UpdateResource_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.UpdateResource(req, "r1");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateResource_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { name = "Updated" });

        var result = await _sut.UpdateResource(req, "r1");

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task UpdateResource_NotFound_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _resourceRepo.GetByIdAsync("r-missing").Returns((Resource?)null);
        var req = TestHelper.CreateRequest(body: new { name = "Updated" });

        var result = await _sut.UpdateResource(req, "r-missing");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateResource_Valid_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _resourceRepo.GetByIdAsync("r1").Returns(new Resource { Id = "r1", Name = "Old" });
        var req = TestHelper.CreateRequest(body: new { name = "New Name" });

        var result = await _sut.UpdateResource(req, "r1");

        Assert.IsType<OkObjectResult>(result);
        await _resourceRepo.Received(1).UpdateAsync(Arg.Is<Resource>(r => r.Name == "New Name"));
    }

    // --- DeleteResource ---

    [Fact]
    public async Task DeleteResource_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.DeleteResource(req, "r1");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeleteResource_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.DeleteResource(req, "r1");

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
        await _resourceRepo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteResource_WithAdminRole_Returns204()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest();

        var result = await _sut.DeleteResource(req, "r1");

        Assert.IsType<NoContentResult>(result);
        await _resourceRepo.Received(1).DeleteAsync("r1");
    }
}
