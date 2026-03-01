using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(238)]
    public class Migration238FixMetadataSourcePlatform : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Platform column defaulted to 0 when rows were first inserted.
            // Backfill the correct enum values so GetByPlatform DB-side filtering works.
            Execute.Sql("UPDATE MetadataSources SET Platform = 1 WHERE Implementation = 'YouTube' AND Platform = 0");
            Execute.Sql("UPDATE MetadataSources SET Platform = 2 WHERE Implementation = 'Twitch'  AND Platform = 0");
        }
    }
}
