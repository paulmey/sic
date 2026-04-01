using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sic.Api.Functions;
using Sic.Core;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Api.Tests;

public class InviteFunctionsTests
{
    private readonly IInviteLinkRepository _inviteRepo = Substitute.For<IInviteLinkRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly InviteService _inviteService;
    private readonly UserService _userService;
    private readonly InviteFunctions _sut;

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

    public InviteFunctionsTests()
    {
        _inviteService = new InviteService(_inviteRepo);
        _userService = new UserService(_userRepo, _inviteRepo);
        _sut = new InviteFunctions(_inviteService, _userService, _inviteRepo, _userRepo);
    }

    [Fact]
    public async Task CreateInvite_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2", body: new { validityDays = 7 });

        var result = await _sut.CreateInvite(req);

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task CreateInvite_WithAdminRole_Returns201()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-1").Returns(_adminUser);
        var req = TestHelper.CreateRequest(body: new { validityDays = 7 });

        var result = await _sut.CreateInvite(req);

        Assert.IsType<CreatedResult>(result);
        await _inviteRepo.Received(1).CreateAsync(Arg.Any<InviteLink>());
    }

    [Fact]
    public async Task GetInvites_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.GetInvites(req);

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task DeleteInvite_WithoutAdminRole_Returns403()
    {
        _userRepo.GetByIdentityAsync("microsoft", "user-2").Returns(_regularUser);
        var req = TestHelper.CreateRequest(userId: "user-2");

        var result = await _sut.DeleteInvite(req, "inv-1");

        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(403, ((StatusCodeResult)result).StatusCode);
        await _inviteRepo.DidNotReceive().DeleteAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task RedeemInvite_WithoutAuth_Returns401()
    {
        var req = TestHelper.CreateAnonymousRequest();

        var result = await _sut.RedeemInvite(req);

        Assert.IsType<UnauthorizedResult>(result);
    }
}
