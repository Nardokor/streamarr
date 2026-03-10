using FluentMigrator;
using Streamarr.Core.Datastore.Migration.Framework;

namespace Streamarr.Core.Datastore.Migration
{
    [Migration(242)]
    public class retention_word_filters : StreamarrMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Channels")
                 .AddColumn("RetentionMembers").AsBoolean().NotNullable().WithDefaultValue(false)
                 .AddColumn("RetentionWatchedWords").AsString().NotNullable().WithDefaultValue(string.Empty)
                 .AddColumn("RetentionIgnoredWords").AsString().NotNullable().WithDefaultValue(string.Empty);

            // Migrate existing RetentionExceptionWords into RetentionWatchedWords
            Execute.Sql("UPDATE \"Channels\" SET \"RetentionWatchedWords\" = \"RetentionExceptionWords\" WHERE \"RetentionExceptionWords\" != ''");

            Delete.Column("RetentionExceptionWords").FromTable("Channels");
        }
    }
}
