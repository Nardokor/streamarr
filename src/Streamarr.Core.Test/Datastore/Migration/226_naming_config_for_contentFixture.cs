using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Datastore.Migration;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Datastore.Migration;

[TestFixture]
public class naming_config_for_contentFixture : MigrationTest<naming_config_for_content>
{
    [Test]
    public void should_recreate_naming_config_with_content_file_format_column()
    {
        // Migration 226 drops the old TV-specific NamingConfig and recreates it for streaming content.
        // Verify the new schema has ContentFileFormat and CreatorFolderFormat columns.
        var db = WithMigrationTestDb();

        var rows = db.Query<NamingConfig226>("SELECT \"ContentFileFormat\", \"CreatorFolderFormat\" FROM \"NamingConfig\"");

        rows.Should().BeEmpty();
    }

    [Test]
    public void should_have_rename_content_column()
    {
        // Verify the RenameContent column was created in the new schema.
        var db = WithMigrationTestDb();

        var rows = db.Query<NamingConfig226Full>("SELECT \"RenameContent\", \"ContentFileFormat\", \"CreatorFolderFormat\" FROM \"NamingConfig\"");

        rows.Should().BeEmpty();
    }
}

internal class NamingConfig226
{
    public string ContentFileFormat { get; set; }
    public string CreatorFolderFormat { get; set; }
}

internal class NamingConfig226Full
{
    public bool RenameContent { get; set; }
    public string ContentFileFormat { get; set; }
    public string CreatorFolderFormat { get; set; }
}
