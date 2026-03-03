using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class channel_archival_rulesFixture : MigrationTest<channel_archival_rules>
{
    [Test]
    public void should_add_retention_days_as_nullable()
    {
        var db = WithMigrationTestDb(c =>
        {
            c.Insert.IntoTable("Channels").Row(new
            {
                CreatorId = 1,
                Platform = 1,
                PlatformId = "UCtest",
                Title = "Test Channel",
                Monitored = true,
                Status = 0,
                DownloadVideos = true,
                DownloadShorts = true,
                DownloadLivestreams = true,
                TitleFilter = string.Empty,
            });
        });

        var channels = db.Query<Channel228>("SELECT \"RetentionDays\", \"PriorityFilter\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().RetentionDays.Should().BeNull();
        channels.First().PriorityFilter.Should().BeEmpty();
    }
}

internal class Channel228
{
    public int? RetentionDays { get; set; }
    public string PriorityFilter { get; set; }
}
