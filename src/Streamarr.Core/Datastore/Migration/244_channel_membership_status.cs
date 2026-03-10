using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(244)]
    public class channel_membership_status : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("MembershipStatus").AsInt32().NotNullable().WithDefaultValue(0)
                 .AddColumn("LastMembershipCheck").AsDateTime().Nullable();
        }
    }
}
