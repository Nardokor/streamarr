using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Import;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Profiles.Qualities;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Import
{
    [TestFixture]
    public class ImportLibraryServiceFixture : CoreTest<ImportLibraryService>
    {
        private string _tempRoot;

        private Creator _creator;
        private Channel _channel;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "streamarr_import_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRoot);

            _creator = new Creator { Id = 1, Title = "Test Creator", Path = Path.Combine(_tempRoot, "Test Creator") };
            _channel = new Channel { Id = 10, CreatorId = 1, PlatformId = "UCtest123", Title = "Test Channel", Platform = PlatformType.YouTube };

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.FindByTitle(It.IsAny<string>()))
                  .Returns((Creator)null);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.AddCreator(It.IsAny<Creator>()))
                  .Returns(_creator);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.CreatorPathExists(It.IsAny<string>()))
                  .Returns(false);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.FindByPlatformId(It.IsAny<PlatformType>(), It.IsAny<string>()))
                  .Returns((Channel)null);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.AddChannel(It.IsAny<Channel>(), It.IsAny<string>()))
                  .Returns(_channel);

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(It.IsAny<int>(), It.IsAny<string>()))
                  .Returns((ContentEntity)null);

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.AddContent(It.IsAny<ContentEntity>()))
                  .Returns<ContentEntity>(c =>
                  {
                      c.Id = 100;
                      return c;
                  });

            Mocker.GetMock<IContentFileService>()
                  .Setup(s => s.AddContentFile(It.IsAny<ContentFile>()))
                  .Returns(new ContentFile { Id = 1 });

            Mocker.GetMock<IQualityProfileService>()
                  .Setup(s => s.All())
                  .Returns(new List<QualityProfile> { new QualityProfile { Id = 1 } });
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        // ── GetImportableFolders ──────────────────────────────────────────────

        [Test]
        public void get_importable_folders_returns_empty_for_nonexistent_root()
        {
            var result = Subject.GetImportableFolders("/nonexistent/path/xyz");

            result.Should().BeEmpty();
        }

        [Test]
        public void get_importable_folders_returns_unclaimed_directories()
        {
            Directory.CreateDirectory(Path.Combine(_tempRoot, "Creator A"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "Creator B"));

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.CreatorPathExists(It.IsAny<string>()))
                  .Returns(false);

            var result = Subject.GetImportableFolders(_tempRoot);

            result.Should().HaveCount(2);
            result.Should().ContainSingle(f => f.FolderName == "Creator A");
            result.Should().ContainSingle(f => f.FolderName == "Creator B");
        }

        [Test]
        public void get_importable_folders_excludes_already_claimed_directories()
        {
            Directory.CreateDirectory(Path.Combine(_tempRoot, "Existing Creator"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "New Creator"));

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.CreatorPathExists(Path.Combine(_tempRoot, "Existing Creator")))
                  .Returns(true);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.CreatorPathExists(Path.Combine(_tempRoot, "New Creator")))
                  .Returns(false);

            var result = Subject.GetImportableFolders(_tempRoot);

            result.Should().HaveCount(1);
            result[0].FolderName.Should().Be("New Creator");
        }

        // ── Import — root path guard ──────────────────────────────────────────

        [Test]
        public void import_returns_empty_result_for_nonexistent_root()
        {
            var result = Subject.Import("/nonexistent/path/xyz", new[] { "Creator A" });

            result.CreatorsCreated.Should().Be(0);
            result.FilesNotMatched.Should().Be(0);
            ExceptionVerification.IgnoreWarns();
        }

        // ── Import — creator matching ─────────────────────────────────────────

        [Test]
        public void import_creates_new_creator_when_not_found()
        {
            var creatorDir = Path.Combine(_tempRoot, "Test Creator");
            Directory.CreateDirectory(creatorDir);

            Subject.Import(_tempRoot, new[] { "Test Creator" });

            Mocker.GetMock<ICreatorService>()
                  .Verify(s => s.AddCreator(It.IsAny<Creator>()), Times.Once);
        }

        [Test]
        public void import_matches_existing_creator_by_title()
        {
            var creatorDir = Path.Combine(_tempRoot, "Test Creator");
            Directory.CreateDirectory(creatorDir);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.FindByTitle("test creator"))
                  .Returns(_creator);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.CreatorsMatched.Should().Be(1);
            Mocker.GetMock<ICreatorService>()
                  .Verify(s => s.AddCreator(It.IsAny<Creator>()), Times.Never);
        }

        // ── Import — no YouTube source ────────────────────────────────────────

        [Test]
        public void import_counts_files_as_not_matched_when_no_youtube_source()
        {
            var creatorDir = Path.Combine(_tempRoot, "Test Creator");
            Directory.CreateDirectory(creatorDir);
            File.WriteAllText(Path.Combine(creatorDir, "Video Title [dQw4w9WgXcQ].mp4"), "fake");

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns((IMetadataSource)null);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.FilesNotMatched.Should().Be(1);
            ExceptionVerification.IgnoreWarns();
        }

        // ── Import — file matching ────────────────────────────────────────────

        [Test]
        public void import_skips_files_without_youtube_id_in_name()
        {
            var creatorDir = Path.Combine(_tempRoot, "Test Creator");
            Directory.CreateDirectory(creatorDir);
            File.WriteAllText(Path.Combine(creatorDir, "video_no_id.mp4"), "fake");

            var mockSource = new Mock<IMetadataSource>();
            mockSource.Setup(s => s.GetContentMetadataBatch(It.IsAny<IEnumerable<string>>()))
                      .Returns(new List<ContentMetadataResult>());

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(mockSource.Object);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.FilesNotMatched.Should().Be(0);
            result.ContentLinked.Should().Be(0);
        }

        [Test]
        public void import_links_file_when_youtube_id_matches_metadata()
        {
            var creatorDir = _creator.Path;
            Directory.CreateDirectory(creatorDir);
            var filePath = Path.Combine(creatorDir, "My Video [dQw4w9WgXcQ].mp4");
            File.WriteAllText(filePath, "fake video content");

            var meta = new ContentMetadataResult
            {
                PlatformContentId = "dQw4w9WgXcQ",
                PlatformChannelId = "UCtest123",
                PlatformChannelTitle = "Test Channel",
                ContentType = ContentType.Video,
                Title = "My Video",
            };

            var mockSource = new Mock<IMetadataSource>();
            mockSource.Setup(s => s.GetContentMetadataBatch(It.IsAny<IEnumerable<string>>()))
                      .Returns(new List<ContentMetadataResult> { meta });

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(mockSource.Object);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.ContentLinked.Should().Be(1);
            result.ChannelsCreated.Should().Be(1);
        }

        [Test]
        public void import_reuses_existing_channel()
        {
            var creatorDir = _creator.Path;
            Directory.CreateDirectory(creatorDir);
            File.WriteAllText(Path.Combine(creatorDir, "My Video [dQw4w9WgXcQ].mp4"), "fake");

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.FindByPlatformId(PlatformType.YouTube, "UCtest123"))
                  .Returns(_channel);

            var meta = new ContentMetadataResult
            {
                PlatformContentId = "dQw4w9WgXcQ",
                PlatformChannelId = "UCtest123",
                ContentType = ContentType.Video,
                Title = "My Video",
            };

            var mockSource = new Mock<IMetadataSource>();
            mockSource.Setup(s => s.GetContentMetadataBatch(It.IsAny<IEnumerable<string>>()))
                      .Returns(new List<ContentMetadataResult> { meta });

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(mockSource.Object);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.ChannelsCreated.Should().Be(0);
            Mocker.GetMock<IChannelService>()
                  .Verify(s => s.AddChannel(It.IsAny<Channel>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void import_increments_already_linked_when_content_has_file()
        {
            var creatorDir = _creator.Path;
            Directory.CreateDirectory(creatorDir);
            File.WriteAllText(Path.Combine(creatorDir, "My Video [dQw4w9WgXcQ].mp4"), "fake");

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.FindByPlatformId(PlatformType.YouTube, "UCtest123"))
                  .Returns(_channel);

            var existingContent = new ContentEntity { Id = 50, ContentFileId = 1 };
            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, "dQw4w9WgXcQ"))
                  .Returns(existingContent);

            var meta = new ContentMetadataResult
            {
                PlatformContentId = "dQw4w9WgXcQ",
                PlatformChannelId = "UCtest123",
                ContentType = ContentType.Video,
                Title = "My Video",
            };

            var mockSource = new Mock<IMetadataSource>();
            mockSource.Setup(s => s.GetContentMetadataBatch(It.IsAny<IEnumerable<string>>()))
                      .Returns(new List<ContentMetadataResult> { meta });

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(mockSource.Object);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.ContentAlreadyLinked.Should().Be(1);
            result.ContentLinked.Should().Be(0);
        }

        [Test]
        public void import_skips_content_with_no_channel_id_in_metadata()
        {
            var creatorDir = _creator.Path;
            Directory.CreateDirectory(creatorDir);
            File.WriteAllText(Path.Combine(creatorDir, "My Video [dQw4w9WgXcQ].mp4"), "fake");

            var meta = new ContentMetadataResult
            {
                PlatformContentId = "dQw4w9WgXcQ",
                PlatformChannelId = string.Empty,
                ContentType = ContentType.Video,
                Title = "My Video",
            };

            var mockSource = new Mock<IMetadataSource>();
            mockSource.Setup(s => s.GetContentMetadataBatch(It.IsAny<IEnumerable<string>>()))
                      .Returns(new List<ContentMetadataResult> { meta });

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(mockSource.Object);

            var result = Subject.Import(_tempRoot, new[] { "Test Creator" });

            result.FilesNotMatched.Should().Be(1);
            result.ContentLinked.Should().Be(0);
            ExceptionVerification.IgnoreWarns();
        }
    }
}
