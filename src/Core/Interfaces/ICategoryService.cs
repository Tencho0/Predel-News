using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface ICategoryService
{
    Task<Category?> GetBySlugAsync(string slug);
    Task<Category?> GetByIdAsync(int id);
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<IReadOnlyList<Category>> GetMainNavigationAsync();
}
