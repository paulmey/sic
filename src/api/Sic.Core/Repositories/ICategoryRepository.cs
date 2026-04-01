using Sic.Core.Models;

namespace Sic.Core.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(string id);
    Task<IEnumerable<Category>> GetAllAsync();
    Task CreateAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(string id);
}
