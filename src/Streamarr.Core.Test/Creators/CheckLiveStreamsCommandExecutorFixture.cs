using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Creators;
using Streamarr.Core.Creators.Commands;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;

namespace Streamarr.Core.Test.Creators
{
    [TestFixture]
    public class CheckLiveStreamsCommandExecutorFixture : CoreTest<CheckLiveStreamsCommandExecutor>
    {
        private Creator _creator;
        private Channel _youtubeChannel;
        private Channel _twitchChannel;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator" };

            _youtubeChannel = new Channel
            {
                Id = 10,
                CreatorId = 1,
                Title = "YouTube Channel",
                Platform = PlatformType.YouTube,
                Monitored = true
            };

            _twitchChannel = new Channel
            {
                Id = 11,
                CreatorId = 1,
                Title = "Twitch Channel",
                Platform = PlatformType.Twitch,
                Monitored = true
            };

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetMonitoredCreators())
                  .Returns(new List<Creator> { _creator });

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetByCreatorId(_creator.Id))
                  .Returns(new List<Channel> { _youtubeChannel, _twitchChannel });
        }

        private void Execute()
        {
            Subject.Execute(new CheckLiveStreamsCommand());
        }

        // ── Channel selection ─────────────────────────────────────────────────

        [Test]
        public void should_check_youtube_channels()
        {
            Execute();

            Mocker.GetMock<ILivestreamStatusService>()
                  .Verify(s => s.RefreshLivestreamStatuses(_youtubeChannel), Times.Once);
        }

        [Test]
        public void should_check_twitch_channels()
        {
            Execute();

            Mocker.GetMock<ILivestreamStatusService>()
                  .Verify(s => s.RefreshLivestreamStatuses(_twitchChannel), Times.Once);
        }

        [Test]
        public void should_skip_unmonitored_channels()
        {
            _youtubeChannel.Monitored = false;
            _twitchChannel.Monitored = false;

            Execute();

            Mocker.GetMock<ILivestreamStatusService>()
                  .Verify(s => s.RefreshLivestreamStatuses(It.IsAny<Channel>()), Times.Never);
        }

        // ── Resilience ────────────────────────────────────────────────────────

        [Test]
        public void should_continue_checking_remaining_channels_when_one_throws()
        {
            Mocker.GetMock<ILivestreamStatusService>()
                  .Setup(s => s.RefreshLivestreamStatuses(_youtubeChannel))
                  .Throws(new Exception("API error"));

            Execute();

            ExceptionVerification.IgnoreWarns();

            Mocker.GetMock<ILivestreamStatusService>()
                  .Verify(s => s.RefreshLivestreamStatuses(_twitchChannel), Times.Once);
        }
    }
}
