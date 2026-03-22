using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PredelNews.Core.Interfaces;

namespace PredelNews.Infrastructure.Repositories;

public class ContactFormRepository : IContactFormRepository
{
    private readonly string _connectionString;

    public ContactFormRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    public async Task InsertAsync(string name, string email, string subject, string message, string? ipAddress)
    {
        const string sql = """
            INSERT INTO pn_contact_submissions (name, email, subject, message, ip_address)
            VALUES (@Name, @Email, @Subject, @Message, @IpAddress)
            """;

        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { Name = name, Email = email, Subject = subject, Message = message, IpAddress = ipAddress ?? "unknown" });
    }
}
