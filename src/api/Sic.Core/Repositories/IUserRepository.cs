using Sic.Core.Models;

namespace Sic.Core.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByIdentityAsync(string identityProvider, string identityId);
    Task<IEnumerable<User>> GetAllAsync();
    Task<int> CountAsync();
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(string id);
}
