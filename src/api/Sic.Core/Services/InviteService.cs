using Sic.Core.Models;
using Sic.Core.Repositories;

namespace Sic.Core.Services;

public class InviteService
{
    private readonly IInviteLinkRepository _inviteRepo;

    public InviteService(IInviteLinkRepository inviteRepo)
    {
        _inviteRepo = inviteRepo;
    }

    public async Task<ServiceResult<InviteLink>> CreateInviteAsync(string createdByUserId, TimeSpan validity, string? resourceId = null)
    {
        var invite = new InviteLink
        {
            Id = Guid.NewGuid().ToString(),
            CreatedByUserId = createdByUserId,
            ResourceId = resourceId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(validity),
            UsedByUserId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _inviteRepo.CreateAsync(invite);
        return ServiceResult<InviteLink>.Ok(invite);
    }

    public async Task<ServiceResult> ValidateInviteAsync(string inviteId)
    {
        var invite = await _inviteRepo.GetByIdAsync(inviteId);
        if (invite is null)
            return ServiceResult.Fail("Invite not found.");

        if (invite.UsedByUserId is not null)
            return ServiceResult.Fail("Invite has already been used.");

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
            return ServiceResult.Fail("Invite has expired.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RedeemInviteAsync(string inviteId, string userId)
    {
        var invite = await _inviteRepo.GetByIdAsync(inviteId);
        if (invite is null)
            return ServiceResult.Fail("Invite not found.");

        if (invite.UsedByUserId is not null)
            return ServiceResult.Fail("Invite has already been used.");

        if (invite.ExpiresAt <= DateTimeOffset.UtcNow)
            return ServiceResult.Fail("Invite has expired.");

        invite.UsedByUserId = userId;
        await _inviteRepo.UpdateAsync(invite);

        return ServiceResult.Ok();
    }
}
