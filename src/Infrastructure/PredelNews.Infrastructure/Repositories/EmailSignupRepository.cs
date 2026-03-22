using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;

namespace PredelNews.Infrastructure.Repositories;

public class EmailSignupRepository : IEmailSignupRepository
{
    private readonly string _connectionString;

    public EmailSignupRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    public async Task<bool> InsertIfNotExistsAsync(string email)
    {
        const string sql = """
            INSERT INTO pn_email_subscribers (email, signed_up_at, consent_flag)
            SELECT @Email, GETUTCDATE(), 1
            WHERE NOT EXISTS (SELECT 1 FROM pn_email_subscribers WHERE email = @Email)
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(sql, new { Email = email });
        return rowsAffected > 0;
    }

    public async Task<IEnumerable<EmailSubscriber>> GetAllAsync()
    {
        const string sql = """
            SELECT id AS Id, email AS Email, signed_up_at AS SignedUpAt, consent_flag AS ConsentFlag
            FROM pn_email_subscribers
            ORDER BY signed_up_at DESC
            """;

        await using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<EmailSubscriber>(sql);
    }

    public async Task<int> GetCountAsync()
    {
        const string sql = "SELECT COUNT(*) FROM pn_email_subscribers";

        await using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql);
    }
}
