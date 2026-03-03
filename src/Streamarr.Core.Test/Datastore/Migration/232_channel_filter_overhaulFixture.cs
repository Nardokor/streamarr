using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class channel_filter_overhaulFixture : MigrationTest<channel_filter_overhaul>
{
    private void InsertChannel(channel_filter_overhaul c, string titleFilter = "", bool recordLiveOnly = false, bool downloadLivestreams = true)
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
            DownloadLivestreams = downloadLivestreams,
            TitleFilter = titleFilter,
            PriorityFilter = string.Empty,
            RecordLiveOnly = recordLiveOnly,
        });
    }

    [Test]
    public void should_migrate_title_filter_to_watched_words()
    {
        var db = WithMigrationTestDb(c => InsertChannel(c, titleFilter: "gaming"));

        var channels = db.Query<Channel232>("SELECT \"WatchedWords\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().WatchedWords.Should().Be("gaming");
    }

    [Test]
    public void should_migrate_record_live_only_to_download_live()
    {
        var db = WithMigrationTestDb(c => InsertChannel(c, recordLiveOnly: true));

        var channels = db.Query<Channel232>("SELECT \"WatchedWords\", \"DownloadLive\", \"DownloadVods\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().DownloadLive.Should().BeTrue();
    }

    [Test]
    public void should_migrate_download_livestreams_to_download_vods()
    {
        var db = WithMigrationTestDb(c => InsertChannel(c, downloadLivestreams: true));

        var channels = db.Query<Channel232>("SELECT \"WatchedWords\", \"DownloadLive\", \"DownloadVods\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().DownloadVods.Should().BeTrue();
    }

    [Test]
    public void should_add_new_filter_columns_with_defaults()
    {
        var db = WithMigrationTestDb(c => InsertChannel(c));

        var channels = db.Query<Channel232Full>(
            "SELECT \"WatchedWords\", \"IgnoredWords\", \"WatchedDefeatsIgnored\", \"AutoDownload\", \"DownloadLive\", \"DownloadVods\" FROM \"Channels\"");

        channels.Should().HaveCount(1);
        channels.First().IgnoredWords.Should().BeEmpty();
        channels.First().WatchedDefeatsIgnored.Should().BeTrue();
        channels.First().AutoDownload.Should().BeTrue();
    }
}

internal class Channel232
{
    public string WatchedWords { get; set; }
    public bool DownloadLive { get; set; }
    public bool DownloadVods { get; set; }
}

internal class Channel232Full
{
    public string WatchedWords { get; set; }
    public string IgnoredWords { get; set; }
    public bool WatchedDefeatsIgnored { get; set; }
    public bool AutoDownload { get; set; }
    public bool DownloadLive { get; set; }
    public bool DownloadVods { get; set; }
}
