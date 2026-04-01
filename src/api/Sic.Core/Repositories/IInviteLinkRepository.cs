using Sic.Core.Models;

namespace Sic.Core.Repositories;

public interface IInviteLinkRepository
{
    Task<InviteLink?> GetByIdAsync(string id);
    Task<IEnumerable<InviteLink>> GetActiveAsync();
    Task CreateAsync(InviteLink invite);
    Task UpdateAsync(InviteLink invite);
    Task DeleteAsync(string id);
}
