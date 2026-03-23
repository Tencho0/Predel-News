using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;

namespace PredelNews.Infrastructure.Repositories;

public class AdSlotRepository : IAdSlotRepository
{
    private readonly string _connectionString;

    public AdSlotRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("umbracoDbDSN")
            ?? throw new InvalidOperationException("Connection string 'umbracoDbDSN' not found.");
    }

    public async Task<IReadOnlyList<AdSlot>> GetAllAsync()
    {
        const string sql = """
            SELECT id AS Id, slot_id AS SlotId, slot_name AS SlotName, mode AS Mode,
                   adsense_code AS AdsenseCode, banner_image_url AS BannerImageUrl,
                   banner_dest_url AS BannerDestUrl, banner_alt_text AS BannerAltText,
                   start_date AS StartDate, end_date AS EndDate, updated_at AS UpdatedAt
            FROM [pn_ad_slots]
            ORDER BY id
            """;
        await using var connection = new SqlConnection(_connectionString);
        var results = await connection.QueryAsync<AdSlot>(sql);
        return results.ToList().AsReadOnly();
    }

    public async Task UpdateAsync(AdSlot slot)
    {
        const string sql = """
            UPDATE [pn_ad_slots]
            SET mode = @Mode,
                adsense_code = @AdsenseCode,
                banner_image_url = @BannerImageUrl,
                banner_dest_url = @BannerDestUrl,
                banner_alt_text = @BannerAltText,
                start_date = @StartDate,
                end_date = @EndDate,
                updated_at = GETUTCDATE()
            WHERE slot_id = @SlotId
            """;
        await using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, slot);
    }
}
