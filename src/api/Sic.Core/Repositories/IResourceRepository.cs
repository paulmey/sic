using Sic.Core.Models;

namespace Sic.Core.Repositories;

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(string id);
    Task<IEnumerable<Resource>> GetAllAsync();
    Task<IEnumerable<Resource>> GetByCategoryAsync(string categoryId);
    Task CreateAsync(Resource resource);
    Task UpdateAsync(Resource resource);
    Task DeleteAsync(string id);
}
