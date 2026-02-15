using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(151)]
    public class remove_custom_filter_type : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Update.Table("CustomFilters").Set(new { Type = "series" }).Where(new { Type = "seriesIndex" });
            Update.Table("CustomFilters").Set(new { Type = "series" }).Where(new { Type = "seriesEditor" });
            Update.Table("CustomFilters").Set(new { Type = "series" }).Where(new { Type = "seasonPass" });
        }
    }
}
