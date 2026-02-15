using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(184)]
    public class remove_invalid_roksbox_metadata_images : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            IfDatabase(ProcessorIdConstants.SQLite).Execute.Sql("DELETE FROM \"MetadataFiles\" WHERE \"Consumer\" = 'RoksboxMetadata' AND \"Type\" = 5 AND (\"RelativePath\" LIKE '%/metadata/%' OR \"RelativePath\" LIKE '%\\metadata\\%')");
            IfDatabase(ProcessorIdConstants.PostgreSQL).Execute.Sql("DELETE FROM \"MetadataFiles\" WHERE \"Consumer\" = 'RoksboxMetadata' AND \"Type\" = 5 AND (\"RelativePath\" LIKE '%/metadata/%' OR \"RelativePath\" LIKE '%\\\\metadata\\\\%')");
        }
    }
}
