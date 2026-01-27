using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface IAuthorService
{
    Task<Author?> GetBySlugAsync(string slug);
    Task<Author?> GetByIdAsync(int id);
    Task<IReadOnlyList<Author>> GetAllAsync();
}
