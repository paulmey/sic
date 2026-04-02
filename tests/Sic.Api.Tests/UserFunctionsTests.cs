using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Tests;

public class UserFunctionsTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IInviteLinkRepository _inviteRepo = Substitute.For<IInviteLinkRepository>();
    private readonly IResourceRoleRepository _roleRepo = Substitute.For<IResourceRoleRepository>();
    private readonly UserService _userService;
    private readonly UserFunctions _sut;

    public UserFunctionsTests()
    {
        _userService = new UserService(_userRepo, _inviteRepo, _roleRepo);
        _sut = new UserFunctions(_userService, _userRepo);
    }

    // --- GetMe ---

    [Fact]
    public async Task GetMe_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.GetMe(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMe_ExistingUser_ReturnsUser()
    {
        var user = new User
        {
            Id = "u1", IdentityProvider = "microsoft", IdentityId = "user-1",
            DisplayName = "Paul"
        };
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(user);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetMe(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<User>(ok.Value);
        Assert.Equal("Paul", returned.DisplayName);
    }

    [Fact]
    public async Task GetMe_FirstUser_CreatesAdminUser()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns((User?)null);
        _userRepo.CountAsync().Returns(0);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetMe(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        await _userRepo.Received(1).CreateAsync(Arg.Is<User>(u => u.AppRoles.Count == 3));
    }

    [Fact]
    public async Task GetMe_NonFirstUserWithoutInvite_Returns400()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns((User?)null);
        _userRepo.CountAsync().Returns(1);
        var req = TestHelper.CreateRequest();

        var result = await _sut.GetMe(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    // --- UpdateMe ---

    [Fact]
    public async Task UpdateMe_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.UpdateMe(req);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task UpdateMe_MissingDisplayName_Returns400()
    {
        var req = TestHelper.CreateRequest(body: new { displayName = "" });

        var result = await _sut.UpdateMe(req);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateMe_UserNotFound_Returns404()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns((User?)null);
        var req = TestHelper.CreateRequest(body: new { displayName = "New Name" });

        var result = await _sut.UpdateMe(req);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateMe_Valid_Returns200()
    {
        var user = new User
        {
            Id = "u1", IdentityProvider = "microsoft", IdentityId = "user-1",
            DisplayName = "Old Name"
        };
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(user);
        var req = TestHelper.CreateRequest(body: new { displayName = "New Name" });

        var result = await _sut.UpdateMe(req);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returned = Assert.IsType<User>(ok.Value);
        Assert.Equal("New Name", returned.DisplayName);
        await _userRepo.Received(1).UpdateAsync(Arg.Is<User>(u => u.DisplayName == "New Name"));
    }
}
