using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Configuration;
using Streamarr.Core.Content;
using Streamarr.Core.Content.Commands;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.RootFolders;
using Streamarr.Core.Test.Framework;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Content
{
    [TestFixture]
    public class RetentionCleanupCommandExecutorFixture : CoreTest<RetentionCleanupCommandExecutor>
    {
        private Channel _channel;
        private Creator _creator;
        private ContentEntity _content;
        private ContentFile _contentFile;
        private Mock<IMetadataSource> _sourceStub;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator", Path = "/creators/test" };

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath("/creators/test"))
                  .Returns("/creators");
            _channel = new Channel
            {
                Id = 10,
                CreatorId = 1,
                Platform = PlatformType.YouTube,
                RetentionDays = 30,
                KeepVideos = false,
            };
            _content = new ContentEntity
            {
                Id = 100,
                ChannelId = 10,
                ContentFileId = 50,
                Title = "Old Video",
                ContentType = ContentType.Video,
                PlatformContentId = "vid123",
                AirDateUtc = DateTime.UtcNow.AddDays(-40), // older than 30-day retention
                Status = ContentStatus.Downloaded,
                Duration = TimeSpan.FromMinutes(10),
            };
            _contentFile = new ContentFile { Id = 50, RelativePath = "OldVideo.mkv" };

            _sourceStub = new Mock<IMetadataSource>();
            _sourceStub.Setup(s => s.GetContentMetadata(It.IsAny<string>()))
                       .Returns(new ContentMetadataResult { Duration = TimeSpan.FromMinutes(10) });

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(_sourceStub.Object);

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetAllDownloaded())
                  .Returns(new List<ContentEntity> { _content });

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetChannel(_channel.Id))
                  .Returns(_channel);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetCreator(_creator.Id))
                  .Returns(_creator);

            Mocker.GetMock<IContentFileService>()
                  .Setup(s => s.GetContentFile(_contentFile.Id))
                  .Returns(_contentFile);

            Mocker.GetMock<IConfigService>()
                  .SetupGet(c => c.DefaultRetentionDays)
                  .Returns(90);
        }

        private void Execute() => Subject.Execute(new RetentionCleanupCommand());

        // ── Retention skipping ────────────────────────────────────────────────

        [Test]
        public void should_skip_channel_when_effective_retention_is_zero()
        {
            _channel.RetentionDays = 0;

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }

        [Test]
        public void should_skip_channel_when_effective_retention_is_negative()
        {
            _channel.RetentionDays = -1;

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }

        [Test]
        public void should_use_global_default_retention_when_channel_retention_not_set()
        {
            _channel.RetentionDays = null;
            Mocker.GetMock<IConfigService>().SetupGet(c => c.DefaultRetentionDays).Returns(0);

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }

        [Test]
        public void should_skip_content_newer_than_cutoff()
        {
            _content.AirDateUtc = DateTime.UtcNow.AddDays(-10); // within 30-day window

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }

        [Test]
        public void should_skip_content_type_marked_always_keep()
        {
            _channel.KeepVideos = true; // videos always kept

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }

        [Test]
        public void should_skip_content_matching_keep_words()
        {
            _channel.RetentionKeepWords = "keep, archive";
            _content.Title = "Archive of old stream";

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.UpdateContent(It.IsAny<ContentEntity>()), Times.Never);
        }

        // ── No file (ContentFileId == 0) ──────────────────────────────────────

        [Test]
        public void should_mark_expired_when_content_has_no_file()
        {
            _content.ContentFileId = 0;

            Execute();

            Mocker.GetMock<IContentService>().Verify(
                s => s.UpdateContent(It.Is<ContentEntity>(c => c.Status == ContentStatus.Expired)),
                Times.Once);
        }

        // ── Platform checks ───────────────────────────────────────────────────

        [Test]
        public void should_mark_deleted_and_keep_file_when_content_gone_from_platform()
        {
            _sourceStub.Setup(s => s.GetContentMetadata(_content.PlatformContentId))
                       .Returns((ContentMetadataResult)null);

            Execute();

            Mocker.GetMock<IContentService>().Verify(
                s => s.UpdateContent(It.Is<ContentEntity>(c => c.Status == ContentStatus.Deleted)),
                Times.Once);
            Mocker.GetMock<IDiskProvider>().Verify(d => d.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void should_mark_modified_when_platform_duration_shrank_more_than_5_percent()
        {
            _content.Duration = TimeSpan.FromSeconds(600);
            _sourceStub.Setup(s => s.GetContentMetadata(_content.PlatformContentId))
                       .Returns(new ContentMetadataResult { Duration = TimeSpan.FromSeconds(500) }); // ~17% shorter

            Execute();

            Mocker.GetMock<IContentService>().Verify(
                s => s.UpdateContent(It.Is<ContentEntity>(c => c.Status == ContentStatus.Modified)),
                Times.Once);
            Mocker.GetMock<IDiskProvider>().Verify(d => d.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        // ── Normal deletion ───────────────────────────────────────────────────

        [Test]
        public void should_move_file_to_recycle_bin_and_mark_available_when_past_retention()
        {
            Execute();

            Mocker.GetMock<IDiskProvider>().Verify(
                d => d.MoveToRecycleBin("/creators/test/OldVideo.mkv", "/creators/.recycle"),
                Times.Once);

            Mocker.GetMock<IContentService>().Verify(
                s => s.UpdateContent(It.Is<ContentEntity>(c =>
                    c.Status == ContentStatus.Available &&
                    c.ContentFileId == 0)),
                Times.Once);
        }
    }
}
