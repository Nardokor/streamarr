using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class streamarr_initialFixture : MigrationTest<streamarr_initial>
{
    [Test]
    public void should_create_creators_table()
    {
        var db = WithMigrationTestDb();

        var rows = db.Query<Creator225>("SELECT \"Id\", \"Title\", \"Monitored\" FROM \"Creators\"");

        rows.Should().BeEmpty();
    }

    [Test]
    public void should_create_channels_table()
    {
        var db = WithMigrationTestDb();

        var rows = db.Query<Channel225>("SELECT \"Id\", \"CreatorId\", \"PlatformId\", \"Title\" FROM \"Channels\"");

        rows.Should().BeEmpty();
    }

    [Test]
    public void should_create_contents_table()
    {
        var db = WithMigrationTestDb();

        var rows = db.Query<Content225>("SELECT \"Id\", \"ChannelId\", \"PlatformContentId\", \"Title\" FROM \"Contents\"");

        rows.Should().BeEmpty();
    }

    [Test]
    public void should_create_content_files_table()
    {
        var db = WithMigrationTestDb();

        var rows = db.Query<ContentFile225>("SELECT \"Id\", \"ContentId\", \"RelativePath\", \"Size\" FROM \"ContentFiles\"");

        rows.Should().BeEmpty();
    }

    [Test]
    public void should_create_creators_table_with_monitored_column()
    {
        // Verify the Creators table includes the Monitored column (no pre-existing data to migrate).
        var db = WithMigrationTestDb();

        var rows = db.Query<Creator225>("SELECT \"Id\", \"Title\", \"Monitored\" FROM \"Creators\"");

        rows.Should().BeEmpty();
    }
}

internal class Creator225
{
    public int Id { get; set; }
    public string Title { get; set; }
    public bool Monitored { get; set; }
}

internal class Channel225
{
    public int Id { get; set; }
    public int CreatorId { get; set; }
    public string PlatformId { get; set; }
    public string Title { get; set; }
}

internal class Content225
{
    public int Id { get; set; }
    public int ChannelId { get; set; }
    public string PlatformContentId { get; set; }
    public string Title { get; set; }
}

internal class ContentFile225
{
    public int Id { get; set; }
    public int ContentId { get; set; }
    public string RelativePath { get; set; }
    public long Size { get; set; }
}
