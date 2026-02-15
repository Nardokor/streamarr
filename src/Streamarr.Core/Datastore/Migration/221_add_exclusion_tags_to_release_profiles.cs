using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(221)]
    public class add_exclusion_tags_to_release_profiles : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ReleaseProfiles").AddColumn("ExcludedTags").AsString().NotNullable().WithDefaultValue("[]");
        }
    }
}
