using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Core.Download;
using Streamarr.Core.Import;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Test.Framework;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Import
{
    [TestFixture]
    public class UrlImportServiceFixture : CoreTest<UrlImportService>
    {
        private const string Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        private Creator _creator;
        private Channel _channel;
        private ContentMetadataResult _meta;
        private Mock<IMetadataSource> _mockSource;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator" };
            _channel = new Channel { Id = 10, CreatorId = 1, PlatformId = "UCtest123", Title = "Test Channel", Platform = PlatformType.YouTube };

            _meta = new ContentMetadataResult
            {
                PlatformContentId = "dQw4w9WgXcQ",
                PlatformChannelId = "UCtest123",
                PlatformChannelTitle = "Test Channel",
                ContentType = ContentType.Video,
                Title = "My Video",
            };

            _mockSource = new Mock<IMetadataSource>();
            _mockSource.Setup(s => s.ResolveFromUrl(It.IsAny<string>())).Returns(_meta);

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(_mockSource.Object);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.FindByPlatformId(PlatformType.YouTube, "UCtest123"))
                  .Returns(_channel);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetChannel(_channel.Id))
                  .Returns(_channel);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetCreator(_creator.Id))
                  .Returns(_creator);

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
        }

        // ── Platform / URL resolution ─────────────────────────────────────────

        [Test]
        public void import_returns_error_for_unrecognized_url()
        {
            var result = Subject.Import("https://example.com/whatever", null);

            result.Status.Should().Be(UrlImportStatus.Error);
        }

        [Test]
        public void import_returns_error_for_non_absolute_url()
        {
            var result = Subject.Import("not-a-url", null);

            result.Status.Should().Be(UrlImportStatus.Error);
        }

        [Test]
        public void import_returns_error_when_no_source_configured_for_platform()
        {
            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns((IMetadataSource)null);

            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.Error);
        }

        [Test]
        public void import_returns_error_when_source_cannot_resolve_url()
        {
            _mockSource.Setup(s => s.ResolveFromUrl(It.IsAny<string>())).Returns((ContentMetadataResult)null);

            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.Error);
        }

        // ── Channel matching ────────────────────────────────────────────────

        [Test]
        public void import_returns_needs_target_when_no_channel_matches()
        {
            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.FindByPlatformId(PlatformType.YouTube, "UCtest123"))
                  .Returns((Channel)null);

            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.NeedsTarget);
            result.ResolvedTitle.Should().Be("My Video");
            result.ResolvedChannelTitle.Should().Be("Test Channel");
        }

        [Test]
        public void import_uses_explicit_channel_id_when_provided()
        {
            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.FindByPlatformId(PlatformType.YouTube, "UCtest123"))
                  .Returns((Channel)null);

            var result = Subject.Import(Url, _channel.Id);

            result.Status.Should().Be(UrlImportStatus.Started);
            Mocker.GetMock<IChannelService>().Verify(s => s.GetChannel(_channel.Id), Times.Once);
        }

        // ── Content creation / duplicate handling ───────────────────────────

        [Test]
        public void import_creates_content_and_queues_download_when_new()
        {
            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.Started);
            result.ContentId.Should().Be(100);
            result.CreatorId.Should().Be(_creator.Id);
            result.ChannelId.Should().Be(_channel.Id);

            Mocker.GetMock<IContentService>()
                  .Verify(s => s.AddContent(It.IsAny<ContentEntity>()), Times.Once);

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.Push(It.Is<DownloadContentCommand>(c => c.ContentId == 100), CommandPriority.Normal, CommandTrigger.Unspecified), Times.Once);
        }

        [Test]
        public void import_returns_already_downloaded_when_content_has_file()
        {
            var existing = new ContentEntity { Id = 50, ChannelId = _channel.Id, ContentFileId = 5, Title = "Existing Video" };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, "dQw4w9WgXcQ"))
                  .Returns(existing);

            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.AlreadyDownloaded);
            result.ContentId.Should().Be(50);

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.Push(It.IsAny<DownloadContentCommand>(), It.IsAny<CommandPriority>(), It.IsAny<CommandTrigger>()), Times.Never);
        }

        [Test]
        public void import_returns_already_downloaded_when_status_is_downloaded_even_without_file_id()
        {
            var existing = new ContentEntity { Id = 51, ChannelId = _channel.Id, ContentFileId = 0, Status = ContentStatus.Downloaded, Title = "Existing Video" };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, "dQw4w9WgXcQ"))
                  .Returns(existing);

            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.AlreadyDownloaded);
        }

        [Test]
        public void import_reuses_existing_content_record_when_not_yet_downloaded()
        {
            var existing = new ContentEntity { Id = 52, ChannelId = _channel.Id, ContentFileId = 0, Status = ContentStatus.Missing, Title = "Existing Video" };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, "dQw4w9WgXcQ"))
                  .Returns(existing);

            var result = Subject.Import(Url, null);

            result.Status.Should().Be(UrlImportStatus.Started);
            result.ContentId.Should().Be(52);

            Mocker.GetMock<IContentService>()
                  .Verify(s => s.AddContent(It.IsAny<ContentEntity>()), Times.Never);

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.Push(It.Is<DownloadContentCommand>(c => c.ContentId == 52), CommandPriority.Normal, CommandTrigger.Unspecified), Times.Once);
        }
    }
}
