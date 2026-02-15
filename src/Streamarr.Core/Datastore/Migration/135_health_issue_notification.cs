using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(135)]
    public class health_issue_notification : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Notifications").AddColumn("OnHealthIssue").AsBoolean().WithDefaultValue(false);
            Alter.Table("Notifications").AddColumn("IncludeHealthWarnings").AsBoolean().WithDefaultValue(false);
        }
    }
}
