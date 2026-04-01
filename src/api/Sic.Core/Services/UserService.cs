using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Core.Services;

public class UserService
{
    private readonly IUserRepository _userRepo;
    private readonly IInviteLinkRepository _inviteRepo;

    public UserService(IUserRepository userRepo, IInviteLinkRepository inviteRepo)
    {
        _userRepo = userRepo;
        _inviteRepo = inviteRepo;
    }

    public async Task<ServiceResult<User>> AuthenticateOrCreateAsync(
        string identityProvider, string identityId, string displayName)
    {
        var existing = await _userRepo.GetByIdentityAsync(identityProvider, identityId);
        if (existing is not null)
            return ServiceResult<User>.Ok(existing);

        var userCount = await _userRepo.CountAsync();
        if (userCount > 0)
            return ServiceResult<User>.Fail("An invite link is required to join.");

        // First user — gets all admin roles
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            IdentityProvider = identityProvider,
            IdentityId = identityId,
            DisplayName = displayName,
            AppRoles = new List<string>(AppRoles.All),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _userRepo.CreateAsync(user);
        return ServiceResult<User>.Ok(user);
    }

    public async Task<ServiceResult<User>> AuthenticateOrCreateWithInviteAsync(
        string identityProvider, string identityId, string displayName, string inviteId)
    {
        var existing = await _userRepo.GetByIdentityAsync(identityProvider, identityId);
        if (existing is not null)
            return ServiceResult<User>.Ok(existing);

        var invite = await _inviteRepo.GetByIdAsync(inviteId);
        if (invite is null)
            return ServiceResult<User>.Fail("Invite not found.");

        if (invite.UsedByUserId is not null)
            return ServiceResult<User>.Fail("Invite has already been used.");

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
            return ServiceResult<User>.Fail("Invite has expired.");

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            IdentityProvider = identityProvider,
            IdentityId = identityId,
            DisplayName = displayName,
            AppRoles = new List<string>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _userRepo.CreateAsync(user);

        invite.UsedByUserId = user.Id;
        await _inviteRepo.UpdateAsync(invite);

        return ServiceResult<User>.Ok(user);
    }
}
