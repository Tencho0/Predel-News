using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PredelNews.Core.Models;
using PredelNews.Core.Repositories;

namespace PredelNews.Infrastructure.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly string _connectionString;

    public CommentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    public async Task<IReadOnlyList<CommentDto>> GetVisibleCommentsAsync(int articleId)
    {
        const string sql = """
            SELECT id AS Id, display_name AS DisplayName, comment_text AS CommentText, created_at AS CreatedAt
            FROM pn_comments
            WHERE article_id = @ArticleId AND is_deleted = 0 AND is_held = 0
            ORDER BY created_at ASC
            """;

        await using var connection = CreateConnection();
        var results = await connection.QueryAsync<CommentDto>(sql, new { ArticleId = articleId });
        return results.ToList();
    }

    public async Task<int> GetCommentCountAsync(int articleId)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM pn_comments
            WHERE article_id = @ArticleId AND is_deleted = 0 AND is_held = 0
            """;

        await using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { ArticleId = articleId });
    }

    public async Task<IReadOnlyDictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> articleIds)
    {
        var idList = articleIds.ToList();
        if (idList.Count == 0)
            return new Dictionary<int, int>();

        const string sql = """
            SELECT article_id AS ArticleId, COUNT(*) AS Count
            FROM pn_comments
            WHERE article_id IN @Ids AND is_deleted = 0 AND is_held = 0
            GROUP BY article_id
            """;

        await using var connection = CreateConnection();
        var results = await connection.QueryAsync<(int ArticleId, int Count)>(sql, new { Ids = idList });
        return results.ToDictionary(r => r.ArticleId, r => r.Count);
    }

    public async Task<CommentDto> InsertCommentAsync(int articleId, string displayName, string commentText,
        string ipAddress, bool isHeld, string? heldReason)
    {
        const string sql = """
            INSERT INTO pn_comments (article_id, display_name, comment_text, ip_address, is_deleted, is_held, held_reason, created_at)
            OUTPUT INSERTED.id, INSERTED.display_name AS DisplayName, INSERTED.comment_text AS CommentText, INSERTED.created_at AS CreatedAt
            VALUES (@ArticleId, @DisplayName, @CommentText, @IpAddress, 0, @IsHeld, @HeldReason, GETUTCDATE())
            """;

        await using var connection = CreateConnection();
        return await connection.QuerySingleAsync<CommentDto>(sql, new
        {
            ArticleId = articleId,
            DisplayName = displayName,
            CommentText = commentText,
            IpAddress = ipAddress,
            IsHeld = isHeld,
            HeldReason = heldReason
        });
    }

    public async Task SoftDeleteAsync(int commentId)
    {
        const string sql = "UPDATE pn_comments SET is_deleted = 1 WHERE id = @Id";

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = commentId });
    }

    public async Task ApproveAsync(int commentId)
    {
        const string sql = "UPDATE pn_comments SET is_held = 0, held_reason = NULL WHERE id = @Id";

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = commentId });
    }

    public async Task<IReadOnlyList<HeldCommentDto>> GetHeldCommentsAsync(int page, int pageSize)
    {
        const string sql = """
            SELECT c.id AS Id, c.article_id AS ArticleId,
                   c.display_name AS DisplayName, c.comment_text AS CommentText,
                   c.held_reason AS HeldReason, c.created_at AS CreatedAt
            FROM pn_comments c
            WHERE c.is_held = 1 AND c.is_deleted = 0
            ORDER BY c.created_at DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        await using var connection = CreateConnection();
        var results = await connection.QueryAsync<HeldCommentDto>(sql, new
        {
            Offset = (page - 1) * pageSize,
            PageSize = pageSize
        });
        return results.ToList();
    }

    public async Task<int> GetHeldCommentsCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM pn_comments WHERE is_held = 1 AND is_deleted = 0";

        await using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task InsertAuditLogAsync(int commentId, string action, int? actingUserId,
        string? actingUsername, string? originalText)
    {
        const string sql = """
            INSERT INTO pn_comment_audit_log (comment_id, action, acting_user_id, acting_username, original_text, created_at)
            VALUES (@CommentId, @Action, @ActingUserId, @ActingUsername, @OriginalText, GETUTCDATE())
            """;

        await using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            CommentId = commentId,
            Action = action,
            ActingUserId = actingUserId,
            ActingUsername = actingUsername,
            OriginalText = originalText
        });
    }

    public async Task<CommentDto?> GetByIdAsync(int commentId)
    {
        const string sql = """
            SELECT id AS Id, display_name AS DisplayName, comment_text AS CommentText, created_at AS CreatedAt
            FROM pn_comments
            WHERE id = @Id
            """;

        await using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<CommentDto?>(sql, new { Id = commentId });
    }
}
