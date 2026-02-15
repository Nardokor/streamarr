using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(211)]
    public class add_custom_colon_replacement_to_naming_config : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("NamingConfig").AddColumn("CustomColonReplacementFormat").AsString().WithDefaultValue("");
        }
    }
}
