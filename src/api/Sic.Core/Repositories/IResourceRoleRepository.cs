using Sic.Core.Models;

namespace Sic.Core.Repositories;

public interface IResourceRoleRepository
{
    Task<IEnumerable<ResourceRole>> GetByResourceAsync(string resourceId);
    Task<ResourceRole?> GetByResourceAndUserAsync(string resourceId, string userId);
    Task CreateAsync(ResourceRole role);
    Task DeleteAsync(string resourceId, string userId);
}
