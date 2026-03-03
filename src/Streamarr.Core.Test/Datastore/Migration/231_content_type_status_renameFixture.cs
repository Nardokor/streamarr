using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class content_type_status_renameFixture : MigrationTest<content_type_status_rename>
{
    [Test]
    public void should_reset_status_5_to_missing()
    {
        var db = WithMigrationTestDb(c =>
        {
            c.Insert.IntoTable("Contents").Row(new
            {
                ChannelId = 1,
                PlatformContentId = "abc123",
                ContentType = 4,
                Title = "Live Stream",
                DateAdded = "2024-01-01T00:00:00Z",
                Monitored = true,
                Status = 5,
            });
        });

        var contents = db.Query<Content231>("SELECT \"Status\" FROM \"Contents\"");

        contents.Should().HaveCount(1);
        contents.First().Status.Should().Be(1);
    }

    [Test]
    public void should_not_change_other_statuses()
    {
        var db = WithMigrationTestDb(c =>
        {
            c.Insert.IntoTable("Contents").Row(new
            {
                ChannelId = 1,
                PlatformContentId = "vid001",
                ContentType = 1,
                Title = "Downloaded Video",
                DateAdded = "2024-01-01T00:00:00Z",
                Monitored = true,
                Status = 4,
            });

            c.Insert.IntoTable("Contents").Row(new
            {
                ChannelId = 1,
                PlatformContentId = "vid002",
                ContentType = 1,
                Title = "Missing Video",
                DateAdded = "2024-01-01T00:00:00Z",
                Monitored = true,
                Status = 1,
            });
        });

        var contents = db.Query<Content231>("SELECT \"Status\" FROM \"Contents\" ORDER BY \"Id\"");

        contents.Should().HaveCount(2);
        contents[0].Status.Should().Be(4);
        contents[1].Status.Should().Be(1);
    }
}

internal class Content231
{
    public int Status { get; set; }
}
