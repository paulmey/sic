using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Api.Tests;

public class ResourceRoleFunctionsTests
{
    private readonly IResourceRoleRepository _roleRepo = Substitute.For<IResourceRoleRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ResourceRoleFunctions _sut;

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

    public ResourceRoleFunctionsTests()
    {
        _sut = new ResourceRoleFunctions(_roleRepo, _userRepo);
    }

    // --- GetResourceRoles ---

    [Fact]
    public async Task GetResourceRoles_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.GetResourceRoles(req, "r1");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetResourceRoles_WithoutAdmin_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");
        var result = await _sut.GetResourceRoles(req, "r1");
        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task GetResourceRoles_WithAdmin_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _roleRepo.GetByResourceAsync("r1").Returns(new List<ResourceRole>());
        var req = TestHelper.CreateRequest();
        var result = await _sut.GetResourceRoles(req, "r1");
        Assert.IsType<OkObjectResult>(result);
    }

    // --- CreateResourceRole ---

    [Fact]
    public async Task CreateResourceRole_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.CreateResourceRole(req, "r1");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task CreateResourceRole_WithoutAdmin_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { userId = "u3", role = "user" });
        var result = await _sut.CreateResourceRole(req, "r1");
        Assert.IsType<StatusCodeResult>(result);
    }

    [Fact]
    public async Task CreateResourceRole_Valid_Returns201()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _roleRepo.GetByResourceAndUserAsync("r1", "u3").Returns((ResourceRole?)null);
        var req = TestHelper.CreateRequest(body: new { userId = "u3", role = "user" });
        var result = await _sut.CreateResourceRole(req, "r1");
        Assert.IsType<CreatedResult>(result);
        await _roleRepo.Received(1).CreateAsync(Arg.Is<ResourceRole>(r => r.UserId == "u3" && r.Role == "user"));
    }

    [Fact]
    public async Task CreateResourceRole_InvalidRole_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { userId = "u3", role = "superadmin" });
        var result = await _sut.CreateResourceRole(req, "r1");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateResourceRole_Duplicate_Returns409()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _roleRepo.GetByResourceAndUserAsync("r1", "u3").Returns(new ResourceRole { Id = "rr1", ResourceId = "r1", UserId = "u3", Role = "user" });
        var req = TestHelper.CreateRequest(body: new { userId = "u3", role = "manager" });
        var result = await _sut.CreateResourceRole(req, "r1");
        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateResourceRole_MissingFields_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { userId = "", role = "" });
        var result = await _sut.CreateResourceRole(req, "r1");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- UpdateResourceRole ---

    [Fact]
    public async Task UpdateResourceRole_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.UpdateResourceRole(req, "r1", "u3");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateResourceRole_NotFound_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _roleRepo.GetByResourceAndUserAsync("r1", "u3").Returns((ResourceRole?)null);
        var req = TestHelper.CreateRequest(body: new { role = "manager" });
        var result = await _sut.UpdateResourceRole(req, "r1", "u3");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateResourceRole_Valid_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _roleRepo.GetByResourceAndUserAsync("r1", "u3").Returns(new ResourceRole { Id = "rr1", ResourceId = "r1", UserId = "u3", Role = "user" });
        var req = TestHelper.CreateRequest(body: new { role = "manager" });
        var result = await _sut.UpdateResourceRole(req, "r1", "u3");
        Assert.IsType<OkObjectResult>(result);
        await _roleRepo.Received(1).UpdateAsync(Arg.Is<ResourceRole>(r => r.Role == "manager"));
    }

    [Fact]
    public async Task UpdateResourceRole_InvalidRole_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _roleRepo.GetByResourceAndUserAsync("r1", "u3").Returns(new ResourceRole { Id = "rr1", ResourceId = "r1", UserId = "u3", Role = "user" });
        var req = TestHelper.CreateRequest(body: new { role = "owner" });
        var result = await _sut.UpdateResourceRole(req, "r1", "u3");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- DeleteResourceRole ---

    [Fact]
    public async Task DeleteResourceRole_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.DeleteResourceRole(req, "r1", "u3");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeleteResourceRole_WithAdmin_Returns204()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest();
        var result = await _sut.DeleteResourceRole(req, "r1", "u3");
        Assert.IsType<NoContentResult>(result);
        await _roleRepo.Received(1).DeleteAsync("r1", "u3");
    }
}
