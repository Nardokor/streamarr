using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class channel_content_filtersFixture : MigrationTest<channel_content_filters>
{
    [Test]
    public void should_add_download_filter_columns_with_defaults()
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
            });
        });

        var channels = db.Query<Channel227>("SELECT \"DownloadVideos\", \"DownloadShorts\", \"DownloadLivestreams\", \"TitleFilter\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().DownloadVideos.Should().BeTrue();
        channels.First().DownloadShorts.Should().BeTrue();
        channels.First().DownloadLivestreams.Should().BeTrue();
        channels.First().TitleFilter.Should().BeEmpty();
    }
}

internal class Channel227
{
    public bool DownloadVideos { get; set; }
    public bool DownloadShorts { get; set; }
    public bool DownloadLivestreams { get; set; }
    public string TitleFilter { get; set; }
}
