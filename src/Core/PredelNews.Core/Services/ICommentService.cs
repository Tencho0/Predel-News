using PredelNews.Core.Models;

namespace PredelNews.Core.Services;

public interface ICommentService
{
    Task<IReadOnlyList<CommentDto>> GetVisibleCommentsAsync(int articleId);

    Task<int> GetCommentCountAsync(int articleId);

    Task<IReadOnlyDictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> articleIds);

    Task<CommentSubmissionResult> SubmitCommentAsync(CommentSubmissionRequest request);

    Task DeleteCommentAsync(int commentId, int? deletedByUserId, string? deletedByUsername);

    Task ApproveCommentAsync(int commentId);

    Task<IReadOnlyList<HeldCommentDto>> GetHeldCommentsAsync(int page = 1, int pageSize = 20);

    Task<int> GetHeldCommentsCountAsync();
}
