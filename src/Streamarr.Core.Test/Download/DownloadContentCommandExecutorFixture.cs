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

            SetupDownloadResult(new YtDlpDownloadResult { Success = true, FilePath = "/media/test/video.mp4", FileSize = 1024 });

            // Live content is routed through the supervisor instead of IYtDlpClient.Download.
            Mocker.GetMock<ILiveRecordingSupervisor>()
                  .Setup(s => s.Supervise(It.IsAny<LiveRecordingRequest>()))
                  .Returns(new YtDlpDownloadResult { Success = true, FilePath = "/media/test/video.mp4", FileSize = 1024, IsMergedOutput = true });

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

        // Sets up IYtDlpClient.Download across its full signature (including the optional
        // onStarted/outputFilename/metadataTitle args the executor passes by name) and invokes
        // the onStarted callback so the executor's active-state transition runs as it would in
        // production. A 6-arg setup silently fails to match the 9-arg call and returns null.
        private void SetupDownloadResult(YtDlpDownloadResult result)
        {
            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      It.IsAny<string>(),
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>()))
                  .Returns((int _, string _, string _, bool _, string _, Action<YtDlpProgress> _, Action onStarted, string _, string _) =>
                  {
                      onStarted?.Invoke();
                      return result;
                  });
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
                      It.IsAny<string>(),
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>()),
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
        public void should_route_live_content_through_supervisor_not_download()
        {
            _content.ContentType = ContentType.Live;

            Execute();

            Mocker.GetMock<ILiveRecordingSupervisor>()
                  .Verify(s => s.Supervise(It.Is<LiveRecordingRequest>(r =>
                      r.ContentId == _content.Id &&
                      r.PlatformContentId == _content.PlatformContentId &&
                      r.Platform == _channel.Platform)),
                  Times.Once);

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      It.IsAny<string>(),
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>()),
                  Times.Never);
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
                      It.IsAny<string>(),
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>()),
                  Times.Once);
        }

        // ── cookiesFilePath from source ───────────────────────────────────────

        [Test]
        public void should_pass_source_cookies_path_to_download_client()
        {
            Mocker.GetMock<IMetadataSource>()
                  .Setup(s => s.CookiesFilePath)
                  .Returns("/config/patreon-cookies.txt");

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      "/config/patreon-cookies.txt",
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>()),
                  Times.Once);
        }

        [Test]
        public void should_pass_null_cookies_when_source_has_no_cookies_file()
        {
            Mocker.GetMock<IMetadataSource>()
                  .Setup(s => s.CookiesFilePath)
                  .Returns((string)null);

            Execute();

            Mocker.GetMock<IYtDlpClient>()
                  .Verify(c => c.Download(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      null,
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>()),
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
            SetupDownloadResult(new YtDlpDownloadResult { Success = false, ErrorMessage = "yt-dlp failed" });

            Execute();

            ExceptionVerification.IgnoreErrors();

            Mocker.GetMock<IContentService>()
                  .Verify(s => s.UpdateContent(It.Is<ContentEntity>(c =>
                      c.Id == _content.Id && c.Status == ContentStatus.Missing)),
                  Times.AtLeastOnce);
        }

        [Test]
        public void should_not_revert_to_recording_when_live_recording_fails()
        {
            // The livestream status service queues a recording with Status already set to
            // Recording. A failed recording (yt-dlp commonly exits non-zero when the stream
            // ends) must NOT restore that transient state, or the content appears stuck
            // recording forever. It should fall back to Missing so it can be re-evaluated.
            _content.ContentType = ContentType.Live;
            _content.Status = ContentStatus.Recording;

            // Record the status at each UpdateContent call. Content is a single mutated
            // reference, so the final status must be captured by value at call time.
            var statusHistory = new System.Collections.Generic.List<ContentStatus>();
            Mocker.GetMock<IContentService>()
                  .Setup(s => s.UpdateContent(It.IsAny<ContentEntity>()))
                  .Callback<ContentEntity>(c => statusHistory.Add(c.Status));

            // The supervisor exhausts its retry budget and returns failure.
            Mocker.GetMock<ILiveRecordingSupervisor>()
                  .Setup(s => s.Supervise(It.IsAny<LiveRecordingRequest>()))
                  .Returns(new YtDlpDownloadResult { Success = false, ErrorMessage = "stream ended" });

            Execute();

            ExceptionVerification.IgnoreErrors();

            Assert.That(statusHistory, Is.Not.Empty);
            Assert.That(
                statusHistory[statusHistory.Count - 1],
                Is.EqualTo(ContentStatus.Missing),
                "A failed live recording must settle on Missing, not the transient Recording state");
        }
    }
}
