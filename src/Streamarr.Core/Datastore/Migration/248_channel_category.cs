using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(248)]
    public class channel_category : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels").AddColumn("Category").AsString().NotNullable().WithDefaultValue(string.Empty);
        }
    }
}
