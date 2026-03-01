using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Core.Creators.Commands;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Creators
{
    [TestFixture]
    public class RefreshCreatorCommandExecutorFixture : CoreTest<RefreshCreatorCommandExecutor>
    {
        private Creator _creator;
        private Channel _channel;
        private Mock<IMetadataSource> _sourceStub;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator" };
            _channel = new Channel
            {
                Id = 10,
                CreatorId = 1,
                Title = "Test Channel",
                Platform = PlatformType.YouTube,
                PlatformId = "UCtest",
                PlatformUrl = "https://youtube.com/@test",
                Monitored = true,
            };

            _sourceStub = new Mock<IMetadataSource>();
            _sourceStub.Setup(s => s.GetNewContent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                       .Returns(new List<ContentMetadataResult>());
            _sourceStub.Setup(s => s.GetChannelMetadata(It.IsAny<string>()))
                       .Returns(new ChannelMetadataResult());

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns(_sourceStub.Object);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetCreator(_creator.Id))
                  .Returns(_creator);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetByCreatorId(_creator.Id))
                  .Returns(new List<Channel> { _channel });

            Mocker.GetMock<IContentFilterService>()
                  .Setup(s => s.PassesFilter(It.IsAny<string>(), It.IsAny<ContentType>(), It.IsAny<Channel>()))
                  .Returns(true);
        }

        private void Execute(int? creatorId = 1)
        {
            Subject.Execute(new RefreshCreatorCommand { CreatorId = creatorId });
        }

        // ── Creator selection ─────────────────────────────────────────────────

        [Test]
        public void should_refresh_specific_creator_when_id_provided()
        {
            Execute(creatorId: _creator.Id);

            Mocker.GetMock<ICreatorService>().Verify(s => s.GetCreator(_creator.Id), Times.Once);
            Mocker.GetMock<ICreatorService>().Verify(s => s.GetMonitoredCreators(), Times.Never);
        }

        [Test]
        public void should_refresh_all_monitored_creators_when_no_id_provided()
        {
            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetMonitoredCreators())
                  .Returns(new List<Creator> { _creator });

            Subject.Execute(new RefreshCreatorCommand { CreatorId = null });

            Mocker.GetMock<ICreatorService>().Verify(s => s.GetMonitoredCreators(), Times.Once);
        }

        // ── Channel skipping ──────────────────────────────────────────────────

        [Test]
        public void should_skip_unmonitored_channel()
        {
            _channel.Monitored = false;

            Execute();

            _sourceStub.Verify(s => s.GetNewContent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Test]
        public void should_skip_channel_when_no_source_configured_for_platform()
        {
            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(PlatformType.YouTube))
                  .Returns((IMetadataSource)null);

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.AddContents(It.IsAny<List<ContentEntity>>()), Times.Never);
        }

        // ── New content sync ──────────────────────────────────────────────────

        [Test]
        public void should_add_new_content_as_missing_when_filter_passes()
        {
            var item = new ContentMetadataResult
            {
                PlatformContentId = "abc123",
                Title = "New Video",
                ContentType = ContentType.Video,
            };

            _sourceStub.Setup(s => s.GetNewContent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                       .Returns(new List<ContentMetadataResult> { item });

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, item.PlatformContentId))
                  .Returns((ContentEntity)null);

            Execute();

            Mocker.GetMock<IContentService>().Verify(
                s => s.AddContents(It.Is<List<ContentEntity>>(l =>
                    l.Count == 1 &&
                    l[0].Status == ContentStatus.Missing &&
                    l[0].PlatformContentId == "abc123")),
                Times.Once);
        }

        [Test]
        public void should_add_new_content_as_unwanted_when_filter_fails()
        {
            var item = new ContentMetadataResult
            {
                PlatformContentId = "xyz789",
                Title = "Filtered Video",
                ContentType = ContentType.Video,
            };

            _sourceStub.Setup(s => s.GetNewContent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                       .Returns(new List<ContentMetadataResult> { item });

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, item.PlatformContentId))
                  .Returns((ContentEntity)null);

            Mocker.GetMock<IContentFilterService>()
                  .Setup(s => s.PassesFilter(item.Title, item.ContentType, _channel))
                  .Returns(false);

            Execute();

            Mocker.GetMock<IContentService>().Verify(
                s => s.AddContents(It.Is<List<ContentEntity>>(l =>
                    l.Count == 1 && l[0].Status == ContentStatus.Unwanted)),
                Times.Once);
        }

        [Test]
        public void should_skip_duplicate_content()
        {
            var item = new ContentMetadataResult { PlatformContentId = "dupe1", Title = "Duplicate" };

            _sourceStub.Setup(s => s.GetNewContent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                       .Returns(new List<ContentMetadataResult> { item });

            // Simulate already-existing content
            Mocker.GetMock<IContentService>()
                  .Setup(s => s.FindByPlatformContentId(_channel.Id, item.PlatformContentId))
                  .Returns(new ContentEntity { PlatformContentId = "dupe1" });

            Execute();

            Mocker.GetMock<IContentService>().Verify(s => s.AddContents(It.IsAny<List<ContentEntity>>()), Times.Never);
        }

        [Test]
        public void should_update_channel_last_info_sync_after_successful_sync()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);

            Execute();

            Mocker.GetMock<IChannelService>().Verify(
                s => s.UpdateChannel(It.Is<Channel>(c => c.LastInfoSync >= before)),
                Times.Once);
        }

        // ── Resilience ────────────────────────────────────────────────────────

        [Test]
        public void should_still_check_livestream_status_when_content_sync_throws()
        {
            _sourceStub.Setup(s => s.GetNewContent(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>()))
                       .Throws(new Exception("API error"));

            Execute();

            ExceptionVerification.IgnoreErrors();

            Mocker.GetMock<ILivestreamStatusService>()
                  .Verify(s => s.RefreshLivestreamStatuses(_channel), Times.Once);
        }
    }
}
