using Umbraco.Cms.Core.Packaging;

namespace PredelNews.Infrastructure.Migrations;

public class PredelNewsMigrationPlan : PackageMigrationPlan
{
    public PredelNewsMigrationPlan()
        : base("PredelNews") { }

    protected override void DefinePlan()
    {
        To<CreateCustomTablesV1Migration>(new Guid("6A1B2C3D-0001-4000-8000-000000000001"));
    }
}
