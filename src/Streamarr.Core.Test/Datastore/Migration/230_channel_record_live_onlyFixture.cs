using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class channel_record_live_onlyFixture : MigrationTest<channel_record_live_only>
{
    [Test]
    public void should_add_record_live_only_defaulting_to_false()
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
                PriorityFilter = string.Empty,
            });
        });

        var channels = db.Query<Channel230>("SELECT \"RecordLiveOnly\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().RecordLiveOnly.Should().BeFalse();
    }
}

internal class Channel230
{
    public bool RecordLiveOnly { get; set; }
}
