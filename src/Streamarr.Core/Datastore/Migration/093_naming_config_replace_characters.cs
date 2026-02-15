using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(93)]
    public class naming_config_replace_illegal_characters : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig").AddColumn("ReplaceIllegalCharacters").AsBoolean().WithDefaultValue(true);
            Update.Table("NamingConfig").Set(new { ReplaceIllegalCharacters = true }).AllRows();
        }
    }
}
