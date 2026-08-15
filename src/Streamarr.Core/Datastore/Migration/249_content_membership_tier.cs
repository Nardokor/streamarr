using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(249)]
    public class content_membership_tier : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Contents")
                 .AddColumn("MembershipTier").AsString().Nullable().WithDefaultValue(null);
        }
    }
}
