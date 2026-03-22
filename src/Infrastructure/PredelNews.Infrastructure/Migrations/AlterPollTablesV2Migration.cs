using Microsoft.Extensions.Logging;
using Umbraco.Cms.Infrastructure.Migrations;

namespace PredelNews.Infrastructure.Migrations;

public class AlterPollTablesV2Migration : AsyncMigrationBase
{
    public AlterPollTablesV2Migration(IMigrationContext context) : base(context) { }

    protected override async Task MigrateAsync()
    {
        Logger.LogInformation("Running PredelNews v2 migration — adding poll table columns");

        Execute.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('pn_polls') AND name = 'open_date')
                ALTER TABLE pn_polls ADD open_date DATETIME NULL
        ").Do();

        Execute.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('pn_polls') AND name = 'created_by_user_id')
                ALTER TABLE pn_polls ADD created_by_user_id INT NOT NULL DEFAULT 0
        ").Do();

        Execute.Sql(@"
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('pn_poll_options') AND name = 'option_order')
                ALTER TABLE pn_poll_options ADD option_order SMALLINT NOT NULL DEFAULT 0
        ").Do();

        await Task.CompletedTask;
    }
}
