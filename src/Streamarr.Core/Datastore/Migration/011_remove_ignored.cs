using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(11)]
    public class remove_ignored : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("Ignored").FromTable("Seasons");
            Delete.Column("Ignored").FromTable("Episodes");
        }
    }
}
