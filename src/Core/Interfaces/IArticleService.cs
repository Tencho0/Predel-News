using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface IArticleService
{
    Task<Article?> GetBySlugAsync(string categorySlug, string articleSlug);
    Task<Article?> GetByIdAsync(int id);
    Task<PagedResult<ArticleSummary>> GetLatestAsync(int page = 1, int pageSize = 20);
    Task<PagedResult<ArticleSummary>> GetByCategoryAsync(string categorySlug, int page = 1, int pageSize = 20);
    Task<PagedResult<ArticleSummary>> GetByTagAsync(string tagSlug, int page = 1, int pageSize = 20);
    Task<PagedResult<ArticleSummary>> GetByAuthorAsync(string authorSlug, int page = 1, int pageSize = 20);
    Task<IReadOnlyList<ArticleSummary>> GetFeaturedAsync(int count = 5);
    Task<IReadOnlyList<ArticleSummary>> GetBreakingNewsAsync(int count = 3);
    Task<IReadOnlyList<ArticleSummary>> GetMostReadAsync(int days = 7, int count = 10);
    Task<IReadOnlyList<ArticleSummary>> GetRelatedAsync(int articleId, int count = 5);
    Task IncrementViewCountAsync(int articleId);
}
