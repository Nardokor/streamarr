using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Core.Creators.Commands;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Test.Framework;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Creators
{
    [TestFixture]
    public class DownloadMissingContentCommandExecutorFixture : CoreTest<DownloadMissingContentCommandExecutor>
    {
        private Creator _creator;
        private Channel _channel;
        private List<ContentEntity> _missingContent;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator" };
            _channel = new Channel
            {
                Id = 10,
                CreatorId = 1,
                Title = "Test Channel",
                Monitored = true,
                AutoDownload = true,
            };
            _missingContent = new List<ContentEntity>
            {
                new ContentEntity { Id = 101, Monitored = true },
                new ContentEntity { Id = 102, Monitored = true },
            };

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetCreator(_creator.Id))
                  .Returns(_creator);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetByCreatorId(_creator.Id))
                  .Returns(new List<Channel> { _channel });

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetMissingContent(_channel.Id))
                  .Returns(_missingContent);
        }

        // ── By channel (manual trigger) ───────────────────────────────────────

        [Test]
        public void should_queue_missing_for_specific_channel_when_channel_id_provided()
        {
            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetChannel(_channel.Id))
                  .Returns(_channel);

            Subject.Execute(new DownloadMissingContentCommand { ChannelId = _channel.Id });

            Mocker.GetMock<IManageCommandQueue>().Verify(
                q => q.PushMany(It.Is<List<DownloadContentCommand>>(l => l.Count == 2)),
                Times.Once);
        }

        // ── By creator ────────────────────────────────────────────────────────

        [Test]
        public void should_queue_missing_for_monitored_channels_under_creator()
        {
            Subject.Execute(new DownloadMissingContentCommand { CreatorId = _creator.Id });

            Mocker.GetMock<IManageCommandQueue>().Verify(
                q => q.PushMany(It.Is<List<DownloadContentCommand>>(l => l.Count == 2)),
                Times.Once);
        }

        [Test]
        public void should_skip_unmonitored_channel()
        {
            _channel.Monitored = false;

            Subject.Execute(new DownloadMissingContentCommand { CreatorId = _creator.Id });

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.PushMany(It.IsAny<List<DownloadContentCommand>>()), Times.Never);
        }

        [Test]
        public void should_skip_channel_with_auto_download_disabled()
        {
            _channel.AutoDownload = false;

            Subject.Execute(new DownloadMissingContentCommand { CreatorId = _creator.Id });

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.PushMany(It.IsAny<List<DownloadContentCommand>>()), Times.Never);
        }

        [Test]
        public void should_not_queue_unmonitored_content_items()
        {
            _missingContent[0].Monitored = false; // first item is unmonitored

            Subject.Execute(new DownloadMissingContentCommand { CreatorId = _creator.Id });

            Mocker.GetMock<IManageCommandQueue>().Verify(
                q => q.PushMany(It.Is<List<DownloadContentCommand>>(l => l.Count == 1)),
                Times.Once);
        }

        [Test]
        public void should_not_call_push_when_no_missing_content()
        {
            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetMissingContent(_channel.Id))
                  .Returns(new List<ContentEntity>());

            Subject.Execute(new DownloadMissingContentCommand { CreatorId = _creator.Id });

            Mocker.GetMock<IManageCommandQueue>()
                  .Verify(q => q.PushMany(It.IsAny<List<DownloadContentCommand>>()), Times.Never);
        }
    }
}
