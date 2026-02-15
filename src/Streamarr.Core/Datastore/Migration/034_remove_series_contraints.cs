using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(34)]
    public class remove_series_contraints : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series")
                .AlterColumn("TvRageId").AsInt32()
                .AlterColumn("ImdbId").AsString().Nullable()
                .AlterColumn("TitleSlug").AsString().Nullable();
        }
    }
}
