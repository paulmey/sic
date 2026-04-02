using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Api.Tests;

public class UserManagementFunctionsTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly UserManagementFunctions _sut;

    private readonly User _adminUser = new()
    {
        Id = "u1", IdentityProvider = "microsoft", IdentityId = "user-1",
        DisplayName = "Admin", AppRoles = new List<string> { AppRoles.UserAdmin }
    };

    private readonly User _regularUser = new()
    {
        Id = "u2", IdentityProvider = "microsoft", IdentityId = "user-2",
        DisplayName = "Regular", AppRoles = new List<string>()
    };

    public UserManagementFunctionsTests()
    {
        _sut = new UserManagementFunctions(_userRepo);
    }

    // --- GetUsers ---

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.GetUsers(req);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetUsers_WithoutAdmin_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");
        var result = await _sut.GetUsers(req);
        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithAdmin_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _userRepo.GetAllAsync().Returns(new List<User> { _adminUser, _regularUser });
        var req = TestHelper.CreateRequest();
        var result = await _sut.GetUsers(req);
        Assert.IsType<OkObjectResult>(result);
    }

    // --- UpdateUser ---

    [Fact]
    public async Task UpdateUser_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.UpdateUser(req, "u2");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WithoutAdmin_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { appRoles = new[] { "user-admin" } });
        var result = await _sut.UpdateUser(req, "u2");
        Assert.IsType<StatusCodeResult>(result);
    }

    [Fact]
    public async Task UpdateUser_NotFound_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _userRepo.GetByIdAsync("u-missing").Returns((User?)null);
        var req = TestHelper.CreateRequest(body: new { appRoles = new[] { "user-admin" } });
        var result = await _sut.UpdateUser(req, "u-missing");
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateUser_ValidRoles_Returns200()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _userRepo.GetByIdAsync("u2").Returns(new User { Id = "u2", DisplayName = "Jane", AppRoles = new List<string>() });
        var req = TestHelper.CreateRequest(body: new { appRoles = new[] { "resource-admin" } });
        var result = await _sut.UpdateUser(req, "u2");
        Assert.IsType<OkObjectResult>(result);
        await _userRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.AppRoles.Contains("resource-admin")));
    }

    [Fact]
    public async Task UpdateUser_InvalidRole_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _userRepo.GetByIdAsync("u2").Returns(new User { Id = "u2", DisplayName = "Jane", AppRoles = new List<string>() });
        var req = TestHelper.CreateRequest(body: new { appRoles = new[] { "superadmin" } });
        var result = await _sut.UpdateUser(req, "u2");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_CannotRemoveOwnUserAdmin_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        _userRepo.GetByIdAsync("u1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { appRoles = new[] { "resource-admin" } });
        var result = await _sut.UpdateUser(req, "u1");
        Assert.IsType<BadRequestObjectResult>(result);
        await _userRepo.DidNotReceive().UpdateAsync(Arg.Any<User>());
    }

    // --- DeleteUser ---

    [Fact]
    public async Task DeleteUser_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();
        var result = await _sut.DeleteUser(req, "u2");
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WithoutAdmin_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");
        var result = await _sut.DeleteUser(req, "u2");
        Assert.IsType<StatusCodeResult>(result);
    }

    [Fact]
    public async Task DeleteUser_SelfDelete_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest();
        var result = await _sut.DeleteUser(req, "u1");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_Valid_Returns204()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest();
        var result = await _sut.DeleteUser(req, "u2");
        Assert.IsType<NoContentResult>(result);
        await _userRepo.Received(1).DeleteAsync("u2");
    }
}
