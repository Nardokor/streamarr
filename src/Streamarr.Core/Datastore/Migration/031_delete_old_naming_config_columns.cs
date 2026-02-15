using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(31)]
    public class delete_old_naming_config_columns : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Delete.Column("Separator")
                  .Column("NumberStyle")
                  .Column("IncludeSeriesTitle")
                  .Column("IncludeEpisodeTitle")
                  .Column("IncludeQuality")
                  .Column("ReplaceSpaces")
                  .FromTable("NamingConfig");
        }
    }
}
