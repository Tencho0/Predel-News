using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace PredelNews.Infrastructure.Migrations;

public class AlterAdSlotsV3Migration : AsyncMigrationBase
{
    public AlterAdSlotsV3Migration(IMigrationContext context) : base(context) { }

    protected override async Task MigrateAsync()
    {
        Logger.LogInformation("Running PredelNews v3 migration — seeding article-bottom ad slot");

        var count = Database.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM [pn_ad_slots] WHERE [slot_id] = 'article-bottom'");

        if (count == 0)
        {
            Database.Execute(
                "INSERT INTO [pn_ad_slots] ([slot_id], [slot_name], [mode], [updated_at]) VALUES (@0, @1, @2, GETUTCDATE())",
                "article-bottom", "Article Bottom (728x90)", "adsense");

            Logger.LogInformation("Seeded article-bottom ad slot");
        }

        await Task.CompletedTask;
    }
}
