using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(13)]
    public class add_air_date_utc : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Episodes").AddColumn("AirDateUtc").AsDateTime().Nullable();

            Execute.Sql("UPDATE \"Episodes\" SET \"AirDateUtc\" = \"AirDate\"");
        }
    }
}
