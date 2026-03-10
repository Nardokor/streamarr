using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(246)]
    public class creator_custom_avatar : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Creators")
                 .AddColumn("CustomThumbnailUrl").AsString().NotNullable().WithDefaultValue(string.Empty);
        }
    }
}
