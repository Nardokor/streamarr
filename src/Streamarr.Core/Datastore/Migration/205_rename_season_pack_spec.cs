using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(205)]
    public class rename_season_pack_spec : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE \"CustomFormats\" SET \"Specifications\" = REPLACE(\"Specifications\", 'SeasonPackSpecification', 'ReleaseTypeSpecification')");
        }
    }
}
