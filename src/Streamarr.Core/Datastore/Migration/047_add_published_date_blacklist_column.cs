using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(47)]
    public class add_temporary_blacklist_columns : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Blacklist").AddColumn("PublishedDate").AsDateTime().Nullable();
        }
    }
}
