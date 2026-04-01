using NSubstitute;
using Sic.Core.Models;
using Sic.Core.Repositories;
using Sic.Core.Services;

namespace Sic.Core.Tests;

public class InviteServiceTests
{
    private readonly IInviteLinkRepository _inviteRepo = Substitute.For<IInviteLinkRepository>();
    private readonly InviteService _sut;

    public InviteServiceTests()
    {
        _sut = new InviteService(_inviteRepo);
    }

    [Fact]
    public async Task CreateInvite_ReturnsValidInvite()
    {
        var result = await _sut.CreateInviteAsync("admin-1", TimeSpan.FromDays(7));

        Assert.True(result.Success);
        var invite = result.Value!;
        Assert.Equal("admin-1", invite.CreatedByUserId);
        Assert.Null(invite.UsedByUserId);
        Assert.True(invite.ExpiresAt > DateTimeOffset.UtcNow);
        await _inviteRepo.Received(1).CreateAsync(Arg.Any<InviteLink>());
    }

    [Fact]
    public async Task ValidateInvite_ValidUnusedInvite_Succeeds()
    {
        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), UsedByUserId = null
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.ValidateInviteAsync("inv-1");

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ValidateInvite_ExpiredInvite_Fails()
    {
        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), UsedByUserId = null
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.ValidateInviteAsync("inv-1");

        Assert.False(result.Success);
        Assert.Contains("expired", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateInvite_AlreadyUsed_Fails()
    {
        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), UsedByUserId = "someone"
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.ValidateInviteAsync("inv-1");

        Assert.False(result.Success);
        Assert.Contains("used", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateInvite_NotFound_Fails()
    {
        _inviteRepo.GetByIdAsync("inv-nonexistent").Returns((InviteLink?)null);

        var result = await _sut.ValidateInviteAsync("inv-nonexistent");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RedeemInvite_ValidInvite_MarksAsUsed()
    {
        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), UsedByUserId = null
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.RedeemInviteAsync("inv-1", "new-user-1");

        Assert.True(result.Success);
        await _inviteRepo.Received(1).UpdateAsync(Arg.Is<InviteLink>(i => i.UsedByUserId == "new-user-1"));
    }

    [Fact]
    public async Task RedeemInvite_NotFound_Fails()
    {
        _inviteRepo.GetByIdAsync("inv-missing").Returns((InviteLink?)null);

        var result = await _sut.RedeemInviteAsync("inv-missing", "user-1");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RedeemInvite_Expired_Fails()
    {
        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1), UsedByUserId = null
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.RedeemInviteAsync("inv-1", "user-1");

        Assert.False(result.Success);
        Assert.Contains("expired", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RedeemInvite_AlreadyUsed_Fails()
    {
        var invite = new InviteLink
        {
            Id = "inv-1", CreatedByUserId = "admin-1",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1), UsedByUserId = "someone"
        };
        _inviteRepo.GetByIdAsync("inv-1").Returns(invite);

        var result = await _sut.RedeemInviteAsync("inv-1", "user-1");

        Assert.False(result.Success);
        Assert.Contains("used", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
