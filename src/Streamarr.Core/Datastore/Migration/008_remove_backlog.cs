using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(8)]
    public class remove_backlog : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("BacklogSetting").FromTable("Series");
            Delete.Column("UseSceneName").FromTable("NamingConfig");
        }
    }
}
