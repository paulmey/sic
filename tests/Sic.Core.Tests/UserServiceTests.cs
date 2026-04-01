using NSubstitute;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Core.Tests;

public class UserServiceTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IInviteLinkRepository _inviteRepo = Substitute.For<IInviteLinkRepository>();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepo, _inviteRepo);
    }

    [Fact]
    public async Task AuthenticateOrCreate_FirstUser_BecomesAdmin()
    {
        _userRepo.GetByIdentityAsync("microsoft", "ms-123").Returns((User?)null);
        _userRepo.CountAsync().Returns(0);

        var result = await _sut.AuthenticateOrCreateAsync("microsoft", "ms-123", "Paul");

        Assert.True(result.Success);
        var user = result.Value!;
        Assert.Contains(AppRoles.UserAdmin, user.AppRoles);
        Assert.Contains(AppRoles.CategoryAdmin, user.AppRoles);
        Assert.Contains(AppRoles.ResourceAdmin, user.AppRoles);
        await _userRepo.Received(1).CreateAsync(Arg.Is<User>(u => u.AppRoles.Count == 3));
    }

    [Fact]
    public async Task AuthenticateOrCreate_ExistingUser_ReturnsWithoutCreating()
    {
        var existing = new User
        {
            Id = "u1", IdentityProvider = "microsoft", IdentityId = "ms-123",
            DisplayName = "Paul", AppRoles = new List<string> { AppRoles.UserAdmin }
        };
        _userRepo.GetByIdentityAsync("microsoft", "ms-123").Returns(existing);

        var result = await _sut.AuthenticateOrCreateAsync("microsoft", "ms-123", "Paul");

        Assert.True(result.Success);
        Assert.Equal("u1", result.Value!.Id);
        await _userRepo.DidNotReceive().CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task AuthenticateOrCreate_NonFirstUserWithoutInvite_Fails()
    {
        _userRepo.GetByIdentityAsync("google", "g-456").Returns((User?)null);
        _userRepo.CountAsync().Returns(1);

        var result = await _sut.AuthenticateOrCreateAsync("google", "g-456", "Jane");

        Assert.False(result.Success);
        Assert.Contains("invite", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthenticateOrCreate_NonFirstUserWithValidInvite_Succeeds()
    {
        _userRepo.GetByIdentityAsync("google", "g-456").Returns((User?)null);

        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), UsedByUserId = null
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.AuthenticateOrCreateWithInviteAsync("google", "g-456", "Jane", "inv-1");

        Assert.True(result.Success);
        Assert.Empty(result.Value!.AppRoles);
        await _userRepo.Received(1).CreateAsync(Arg.Is<User>(u => u.AppRoles.Count == 0));
        await _inviteRepo.Received(1).UpdateAsync(Arg.Is<InviteLink>(i => i.UsedByUserId != null));
    }
}
