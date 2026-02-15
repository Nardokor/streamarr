using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;
using Streamarr.Core.Languages;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(176)]
    public class original_language : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series")
                .AddColumn("OriginalLanguage").AsInt32().WithDefaultValue((int)Language.English);
        }
    }
}
