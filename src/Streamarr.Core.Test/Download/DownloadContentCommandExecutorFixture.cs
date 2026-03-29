using System;
using Moq;
using NUnit.Framework;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Download;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Test.Framework;
using Streamarr.Test.Common;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Test.Download
{
    [TestFixture]
    public class DownloadContentCommandExecutorFixture : CoreTest<DownloadContentCommandExecutor>
    {
        private Creator _creator;
        private Channel _channel;
        private ContentEntity _content;

        [SetUp]
        public void SetUp()
        {
            _creator = new Creator { Id = 1, Title = "Test Creator", Path = "/media/test" };

            _channel = new Channel
            {
                Id = 10,
                CreatorId = 1,
                Title = "Test Channel",
                Platform = PlatformType.YouTube
            };

            _content = new ContentEntity
            {
                Id = 100,
                ChannelId = 10,
                Title = "Test Video",
                PlatformContentId = "dQw4w9WgXcQ",
                ContentType = ContentType.Video,
                Status = ContentStatus.Missing
            };

            Mocker.GetMock<IContentService>()
                  .Setup(s => s.GetContent(_content.Id))
                  .Returns(_content);

            Mocker.GetMock<IChannelService>()
                  .Setup(s => s.GetChannel(_channel.Id))
                  .Returns(_channel);

            Mocker.GetMock<ICreatorService>()
                  .Setup(s => s.GetCreator(_creator.Id))
                  .Returns(_creator);

            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.Download(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Action<YtDlpProgress>>()))
                  .Returns(new YtDlpDownloadResult { Success = true, FilePath = "/media/test/video.mp4", FileSize = 1024 });

            Mocker.GetMock<IContentFileService>()
                  .Setup(s => s.AddContentFile(It.IsAny<ContentFile>()))
                  .Returns(new ContentFile { Id = 1 });

            // Default: factory returns a source whose GetDownloadUrl echoes a YouTube URL.
            // Individual URL tests override GetDownloadUrl on the mock directly.
            var mockSource = Mocker.GetMock<IMetadataSource>();
            mockSource
                .Setup(s => s.GetDownloadUrl(It.IsAny<string>()))
                .Returns((string id) => $"https://www.youtube.com/watch?v={id}");
            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(It.IsAny<PlatformType>()))
                  .Returns(mockSource.Object);
        }

        private void Execute()
        {
            Subject.Execute(new DownloadContentCommand { ContentId = _content.Id });
        }

        // ── Download URL building ─────────────────────────────────────────────

        [Test]
        public void should_pass_url_from_source_to_download_client()
        {
            const string expectedUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
            _content.PlatformContentId = "dQw4w9WgXcQ";

            Mocker.GetMock<IMetadataSource>()
                  .Setup(s => s.GetDownloadUrl("dQw4w9WgXcQ"))
                  .Returns(expectedUrl);

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      _content.Id,
                      expectedUrl,
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      It.IsAny<bool>(),
                      It.IsAny<Action<YtDlpProgress>>()),
                  Times.Once);
        }

        [Test]
        public void should_look_up_source_by_channel_platform()
        {
            _channel.Platform = PlatformType.Twitch;

            Execute();

            Mocker.GetMock<IMetadataSourceFactory>()
                  .Verify(f => f.GetByPlatform(PlatformType.Twitch), Times.Once);
        }

        // ── isLive flag ───────────────────────────────────────────────────────

        [Test]
        public void should_pass_is_live_true_for_live_content_type()
        {
            _content.ContentType = ContentType.Live;

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      true,
                      It.IsAny<bool>(),
                      It.IsAny<Action<YtDlpProgress>>()),
                  Times.Once);
        }

        [Test]
        public void should_pass_is_live_false_for_regular_video()
        {
            _content.ContentType = ContentType.Video;

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      false,
                      It.IsAny<bool>(),
                      It.IsAny<Action<YtDlpProgress>>()),
                  Times.Once);
        }

        // ── needsCookies flag ─────────────────────────────────────────────────

        [Test]
        public void should_pass_needs_cookies_true_for_members_content()
        {
            _content.IsMembers = true;

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      true,
                      It.IsAny<Action<YtDlpProgress>>()),
                  Times.Once);
        }

        [Test]
        public void should_pass_needs_cookies_false_for_public_content()
        {
            _content.IsMembers = false;

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      false,
                      It.IsAny<Action<YtDlpProgress>>()),
                  Times.Once);
        }

        // ── Status transitions ────────────────────────────────────────────────

        [Test]
        public void should_set_status_to_downloaded_on_success()
        {
            Execute();

            Mocker.GetMock<IContentService>()
                  .Verify(s => s.UpdateContent(It.Is<ContentEntity>(c =>
                      c.Id == _content.Id && c.Status == ContentStatus.Downloaded)),
                  Times.AtLeastOnce);
        }

        [Test]
        public void should_set_status_to_missing_on_failure()
        {
            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.Download(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<Action<YtDlpProgress>>()))
                  .Returns(new YtDlpDownloadResult { Success = false, ErrorMessage = "yt-dlp failed" });

            Execute();

            ExceptionVerification.IgnoreErrors();

            Mocker.GetMock<IContentService>()
                  .Verify(s => s.UpdateContent(It.Is<ContentEntity>(c =>
                      c.Id == _content.Id && c.Status == ContentStatus.Missing)),
                  Times.AtLeastOnce);
        }
    }
}
