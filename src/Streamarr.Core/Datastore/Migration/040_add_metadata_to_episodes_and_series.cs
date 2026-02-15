using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(40)]
    public class add_metadata_to_episodes_and_series : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Series")
                 .AddColumn("Actors").AsString().Nullable()
                 .AddColumn("Ratings").AsString().Nullable()
                 .AddColumn("Genres").AsString().Nullable()
                 .AddColumn("Certification").AsString().Nullable();

            Alter.Table("Episodes")
                 .AddColumn("Ratings").AsString().Nullable()
                 .AddColumn("Images").AsString().Nullable();
        }
    }
}
