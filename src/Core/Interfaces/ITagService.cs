using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface ITagService
{
    Task<Tag?> GetBySlugAsync(string slug);
    Task<Tag?> GetByIdAsync(int id);
    Task<IReadOnlyList<Tag>> GetAllAsync();
    Task<IReadOnlyList<Tag>> GetPopularAsync(int count = 20);
}
