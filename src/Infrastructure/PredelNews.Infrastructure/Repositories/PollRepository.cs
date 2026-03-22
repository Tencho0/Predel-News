using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;

namespace PredelNews.Infrastructure.Repositories;

public class PollRepository : IPollRepository
{
    private readonly string _connectionString;

    public PollRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    public async Task<Poll?> GetActivePollWithOptionsAsync()
    {
        const string sql = """
            SELECT id AS Id, question AS Question, is_active AS IsActive,
                   open_date AS OpensAt, closed_at AS ClosesAt,
                   created_by_user_id AS CreatedByUserId, created_at AS CreatedAt
            FROM pn_polls WHERE is_active = 1;

            SELECT o.id AS Id, o.poll_id AS PollId, o.option_text AS OptionText,
                   o.vote_count AS VoteCount, o.option_order AS SortOrder
            FROM pn_poll_options o
            INNER JOIN pn_polls p ON p.id = o.poll_id AND p.is_active = 1
            ORDER BY o.option_order;
            """;

        await using var connection = new SqlConnection(_connectionString);
        using var multi = await connection.QueryMultipleAsync(sql);
        var poll = await multi.ReadSingleOrDefaultAsync<Poll>();
        if (poll == null) return null;

        poll.Options = (await multi.ReadAsync<PollOption>()).ToList();
        return poll;
    }

    public async Task IncrementVoteAsync(int pollId, int optionId)
    {
        const string sql = """
            UPDATE pn_poll_options SET vote_count = vote_count + 1
            WHERE id = @OptionId AND poll_id = @PollId
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { OptionId = optionId, PollId = pollId });
    }

    public async Task<IEnumerable<PollOptionResult>> GetResultsAsync(int pollId)
    {
        const string sql = """
            SELECT id AS OptionId, option_text AS OptionText, vote_count AS VoteCount,
                   CASE WHEN (SELECT SUM(vote_count) FROM pn_poll_options WHERE poll_id = @PollId) = 0 THEN 0
                        ELSE CAST(vote_count * 100.0 / (SELECT SUM(vote_count) FROM pn_poll_options WHERE poll_id = @PollId) AS DECIMAL(5,1))
                   END AS Percentage
            FROM pn_poll_options WHERE poll_id = @PollId
            ORDER BY option_order
            """;

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<PollOptionResult>(sql, new { PollId = pollId });
    }

    public async Task<int> CreatePollAsync(string question, int createdByUserId, DateTime? opensAt, DateTime? closesAt)
    {
        const string sql = """
            INSERT INTO pn_polls (question, is_active, open_date, closed_at, created_by_user_id)
            OUTPUT INSERTED.id
            VALUES (@Question, 0, @OpensAt, @ClosesAt, @CreatedByUserId)
            """;

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, new { Question = question, OpensAt = opensAt, ClosesAt = closesAt, CreatedByUserId = createdByUserId });
    }

    public async Task AddOptionAsync(int pollId, string optionText, int sortOrder)
    {
        const string sql = """
            INSERT INTO pn_poll_options (poll_id, option_text, vote_count, option_order)
            VALUES (@PollId, @OptionText, 0, @SortOrder)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { PollId = pollId, OptionText = optionText, SortOrder = sortOrder });
    }

    public async Task ActivatePollAsync(int pollId)
    {
        const string sql = """
            UPDATE pn_polls SET is_active = 0 WHERE is_active = 1;
            UPDATE pn_polls SET is_active = 1 WHERE id = @PollId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(sql, new { PollId = pollId }, transaction: tx);
        await tx.CommitAsync();
    }

    public async Task DeactivatePollAsync(int pollId)
    {
        const string sql = "UPDATE pn_polls SET is_active = 0 WHERE id = @PollId";

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { PollId = pollId });
    }

    public async Task<IEnumerable<Poll>> GetAllPollsAsync()
    {
        const string sql = """
            SELECT id AS Id, question AS Question, is_active AS IsActive,
                   open_date AS OpensAt, closed_at AS ClosesAt,
                   created_by_user_id AS CreatedByUserId, created_at AS CreatedAt
            FROM pn_polls ORDER BY created_at DESC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var polls = (await connection.QueryAsync<Poll>(sql)).ToList();

        if (polls.Count == 0) return polls;

        var pollIds = polls.Select(p => p.Id);
        const string optionsSql = """
            SELECT id AS Id, poll_id AS PollId, option_text AS OptionText,
                   vote_count AS VoteCount, option_order AS SortOrder
            FROM pn_poll_options WHERE poll_id IN @PollIds
            ORDER BY option_order
            """;

        var options = (await connection.QueryAsync<PollOption>(optionsSql, new { PollIds = pollIds })).ToList();
        foreach (var poll in polls)
            poll.Options = options.Where(o => o.PollId == poll.Id).ToList();

        return polls;
    }

    public async Task<Poll?> GetPollWithOptionsAsync(int pollId)
    {
        const string sql = """
            SELECT id AS Id, question AS Question, is_active AS IsActive,
                   open_date AS OpensAt, closed_at AS ClosesAt,
                   created_by_user_id AS CreatedByUserId, created_at AS CreatedAt
            FROM pn_polls WHERE id = @PollId;

            SELECT id AS Id, poll_id AS PollId, option_text AS OptionText,
                   vote_count AS VoteCount, option_order AS SortOrder
            FROM pn_poll_options WHERE poll_id = @PollId ORDER BY option_order;
            """;

        await using var connection = new SqlConnection(_connectionString);
        using var multi = await connection.QueryMultipleAsync(sql, new { PollId = pollId });
        var poll = await multi.ReadSingleOrDefaultAsync<Poll>();
        if (poll == null) return null;

        poll.Options = (await multi.ReadAsync<PollOption>()).ToList();
        return poll;
    }

    public async Task DeletePollAsync(int pollId)
    {
        const string sql = """
            DELETE FROM pn_poll_options WHERE poll_id = @PollId;
            DELETE FROM pn_polls WHERE id = @PollId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(sql, new { PollId = pollId }, transaction: tx);
        await tx.CommitAsync();
    }

    public async Task<bool> HasVotesAsync(int pollId)
    {
        const string sql = "SELECT CASE WHEN EXISTS (SELECT 1 FROM pn_poll_options WHERE poll_id = @PollId AND vote_count > 0) THEN 1 ELSE 0 END";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<bool>(sql, new { PollId = pollId });
    }
}
