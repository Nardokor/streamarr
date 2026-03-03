using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class download_historyFixture : MigrationTest<download_history>
{
    [Test]
    public void should_recreate_download_history_with_streamarr_schema()
    {
        // Migration 229 drops the old DownloadHistory table entirely and recreates it.
        // Verify the new schema has the expected Streamarr columns.
        var db = WithMigrationTestDb();

        var rows = db.Query<DownloadHistory229>("SELECT \"ContentId\", \"ChannelId\", \"CreatorId\", \"Title\", \"EventType\" FROM \"DownloadHistory\"");

        rows.Should().BeEmpty();
    }
}

internal class DownloadHistory229
{
    public int ContentId { get; set; }
    public int ChannelId { get; set; }
    public int CreatorId { get; set; }
    public string Title { get; set; }
    public int EventType { get; set; }
}
