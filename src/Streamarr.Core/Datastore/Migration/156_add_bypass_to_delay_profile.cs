using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(156)]
    public class add_bypass_to_delay_profile : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DelayProfiles").AddColumn("BypassIfHighestQuality").AsBoolean().WithDefaultValue(false);

            // Set to true for existing Delay Profiles to keep behavior the same.
            Update.Table("DelayProfiles").Set(new { BypassIfHighestQuality = true }).AllRows();
        }
    }
}
