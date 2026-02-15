using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(168)]
    public class add_additional_info_to_pending_releases : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("PendingReleases").AddColumn("AdditionalInfo").AsString().Nullable();
        }
    }
}
