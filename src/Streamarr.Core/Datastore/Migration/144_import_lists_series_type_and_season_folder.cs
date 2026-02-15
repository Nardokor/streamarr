using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(144)]
    public class import_lists_series_type_and_season_folder : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("ImportLists").AddColumn("SeriesType").AsInt32().WithDefaultValue(0);
            Alter.Table("ImportLists").AddColumn("SeasonFolder").AsBoolean().WithDefaultValue(true);
        }
    }
}
