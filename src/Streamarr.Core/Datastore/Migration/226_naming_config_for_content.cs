using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(226)]
    public class naming_config_for_content : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            // Remove TV-specific columns
            Delete.Column("UseSceneName")
                  .Column("MultiEpisodeStyle")
                  .Column("RenameEpisodes")
                  .Column("StandardEpisodeFormat")
                  .Column("DailyEpisodeFormat")
                  .Column("AnimeEpisodeFormat")
                  .Column("SeriesFolderFormat")
                  .Column("SeasonFolderFormat")
                  .Column("SpecialsFolderFormat")
                  .Column("CustomColonReplacementFormat")
                  .FromTable("NamingConfig");

            // Add streaming content columns
            Alter.Table("NamingConfig")
                 .AddColumn("RenameContent").AsBoolean().WithDefaultValue(true);

            Alter.Table("NamingConfig")
                 .AddColumn("ContentFileFormat").AsString().WithDefaultValue("{Published Date} - {Content Title} [{Content Id}]");

            Alter.Table("NamingConfig")
                 .AddColumn("CreatorFolderFormat").AsString().WithDefaultValue("{Creator Title}");
        }
    }
}
