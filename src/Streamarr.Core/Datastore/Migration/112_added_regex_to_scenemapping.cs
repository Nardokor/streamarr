using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(112)]
    public class added_regex_to_scenemapping : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("SceneMappings").AddColumn("FilterRegex").AsString().Nullable();
        }
    }
}
