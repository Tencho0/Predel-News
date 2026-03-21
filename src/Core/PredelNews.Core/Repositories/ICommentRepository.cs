using PredelNews.Core.Models;

namespace PredelNews.Core.Repositories;

public interface ICommentRepository
{
    Task<IReadOnlyList<CommentDto>> GetVisibleCommentsAsync(int articleId);

    Task<int> GetCommentCountAsync(int articleId);

    Task<IReadOnlyDictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> articleIds);

    Task<CommentDto> InsertCommentAsync(int articleId, string displayName, string commentText,
        string ipAddress, bool isHeld, string? heldReason);

    Task SoftDeleteAsync(int commentId);

    Task ApproveAsync(int commentId);

    Task<IReadOnlyList<HeldCommentDto>> GetHeldCommentsAsync(int page, int pageSize);

    Task<int> GetHeldCommentsCountAsync();

    Task InsertAuditLogAsync(int commentId, string action, int? actingUserId,
        string? actingUsername, string? originalText);

    Task<CommentDto?> GetByIdAsync(int commentId);
}
