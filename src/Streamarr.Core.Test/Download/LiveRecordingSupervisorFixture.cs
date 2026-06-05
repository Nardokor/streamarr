using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Configuration;
using Streamarr.Core.Content;
using Streamarr.Core.Download;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Download
{
    [TestFixture]
    public class LiveRecordingSupervisorFixture : CoreTest<LiveRecordingSupervisor>
    {
        private TestClock _clock;
        private LiveRecordingRequest _request;

        [SetUp]
        public void SetUp()
        {
            _clock = new TestClock();
            Mocker.SetConstant<ILiveRetryClock>(_clock);

            _request = new LiveRecordingRequest
            {
                ContentId = 100,
                PlatformContentId = "live123",
                Platform = PlatformType.YouTube,
                Url = "https://www.youtube.com/watch?v=live123",
                OutputPath = "/media/test",
            };

            // The supervisor acquires a concurrency slot for the whole recording.
            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.AcquireDownloadSlot())
                  .Returns(Mock.Of<IDisposable>());

            // Config defaults: 15s backoff, 10 consecutive failures, 30 min window.
            Mocker.GetMock<IConfigService>().Setup(c => c.YtDlpLiveRetryBackoffSeconds).Returns(15);
            Mocker.GetMock<IConfigService>().Setup(c => c.YtDlpLiveMaxConsecutiveFailures).Returns(10);
            Mocker.GetMock<IConfigService>().Setup(c => c.YtDlpLiveMaxRetryWindowMinutes).Returns(30);

            // Default source for live-status probes.
            var source = Mocker.GetMock<IMetadataSource>();
            source.Setup(s => s.LivestreamDelegatePlatform).Returns(PlatformType.YouTube);
            Mocker.GetMock<IMetadataSourceFactory>()
                  .Setup(f => f.GetByPlatform(It.IsAny<PlatformType>()))
                  .Returns(source.Object);
        }

        // ── helpers ───────────────────────────────────────────────────────────

        // Scripts a sequence of yt-dlp attempt results; the last entry repeats once exhausted.
        private void ScriptDownloads(params YtDlpDownloadResult[] results)
        {
            var index = 0;
            Mocker.GetMock<IYtDlpClient>()
                  .Setup(c => c.DownloadHeld(
                      It.IsAny<int>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>(),
                      It.IsAny<string>(),
                      It.IsAny<Action<YtDlpProgress>>(),
                      It.IsAny<Action>(),
                      It.IsAny<string>(),
                      It.IsAny<string>(),
                      It.IsAny<bool>()))
                  .Returns(
                      (int id,
                       string url,
                       string outputPath,
                       bool isLive,
                       string cookies,
                       Action<YtDlpProgress> onProgress,
                       Action onStarted,
                       string outFile,
                       string metaTitle,
                       bool keepPartials) =>
                  {
                      onStarted?.Invoke();
                      var result = results[Math.Min(index, results.Length - 1)];
                      index++;
                      return result;
                  });
        }

        private void SetupProbe(params ContentType[] sequence)
        {
            var index = 0;
            Mocker.GetMock<IMetadataSource>()
                  .Setup(s => s.GetLivestreamStatusUpdates(It.IsAny<IEnumerable<string>>()))
                  .Returns(() =>
                  {
                      var type = sequence[Math.Min(index, sequence.Length - 1)];
                      index++;
                      return new[]
                      {
                          new ContentStatusUpdate { PlatformContentId = _request.PlatformContentId, NewContentType = type }
                      };
                  });
        }

        // A clean, complete capture: yt-dlp merged a final file with no network errors.
        private static YtDlpDownloadResult Merged() =>
            new YtDlpDownloadResult { Success = true, IsMergedOutput = true, FilePath = "/media/test/live.mp4", FileSize = 5000 };

        // yt-dlp exited 0 and merged a file, but its stderr showed a dropped connection — the file
        // is almost certainly truncated.
        private static YtDlpDownloadResult Interrupted() =>
            new YtDlpDownloadResult { Success = true, IsMergedOutput = true, WasInterrupted = true, FilePath = "/media/test/live.mp4", FileSize = 1000 };

        private static YtDlpDownloadResult Failed() =>
            new YtDlpDownloadResult { Success = false, ErrorMessage = "interrupted" };

        private int DownloadCallCount() => Mocker.GetMock<IYtDlpClient>().Invocations
            .Count(i => i.Method.Name == nameof(IYtDlpClient.DownloadHeld));

        // Make the truncated output file appear to exist so DeleteIfExists actually deletes it.
        private void OutputFileExists() =>
            Mocker.GetMock<IDiskProvider>().Setup(d => d.FileExists("/media/test/live.mp4")).Returns(true);

        // ── tests ─────────────────────────────────────────────────────────────

        [Test]
        public void should_accept_clean_capture_when_stream_has_ended()
        {
            ScriptDownloads(Merged());
            SetupProbe(ContentType.Vod);

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.True);
            Assert.That(result.IsMergedOutput, Is.True);
            Assert.That(DownloadCallCount(), Is.EqualTo(1));
        }

        [Test]
        public void should_resume_when_yt_dlp_succeeds_but_stream_is_still_live()
        {
            // The core bug: yt-dlp exits 0 with a merged (truncated) file on an interruption, but
            // the platform says the stream is still live. The supervisor must NOT accept it as
            // complete — it must delete the truncated file and relaunch.
            ScriptDownloads(Merged(), Merged());
            SetupProbe(ContentType.Live, ContentType.Vod);
            OutputFileExists();

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.True);
            Assert.That(DownloadCallCount(), Is.EqualTo(2), "a still-live 'success' must trigger a relaunch");
            Mocker.GetMock<IDiskProvider>()
                  .Verify(d => d.DeleteFile("/media/test/live.mp4"), Times.AtLeastOnce, "the truncated capture must be removed before relaunch");
        }

        [Test]
        public void should_recapture_when_stream_ends_during_an_interruption()
        {
            // Nasty edge: yt-dlp finalizes a truncated file AND the stream ends during the outage,
            // so the probe reports Ended. WasInterrupted tells us the capture is short, so the
            // supervisor recaptures the full archive instead of accepting the truncated file.
            ScriptDownloads(Interrupted(), Merged());
            SetupProbe(ContentType.Vod);
            OutputFileExists();

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.True);
            Assert.That(result.WasInterrupted, Is.False, "final result should be the clean recapture");
            Assert.That(DownloadCallCount(), Is.EqualTo(2), "interrupted capture at stream-end must be recaptured");
        }

        [Test]
        public void should_relaunch_on_failure_until_clean_merge()
        {
            // Two failed attempts while still live, then a clean merge once the stream ends.
            ScriptDownloads(Failed(), Failed(), Merged());
            SetupProbe(ContentType.Live, ContentType.Live, ContentType.Vod);

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.True);
            Assert.That(DownloadCallCount(), Is.EqualTo(3));
            Assert.That(_clock.WaitCount, Is.EqualTo(2), "should back off before each of the 2 relaunches");
        }

        [Test]
        public void should_recapture_when_first_attempt_fails_and_stream_ended()
        {
            // Attempt fails without a merge; probe says the stream is now a VOD → one recapture
            // pass (which succeeds) and no relaunch loop.
            ScriptDownloads(Failed(), Merged());
            SetupProbe(ContentType.Vod);

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.True);

            // 1 failed attempt + 1 recapture attempt.
            Assert.That(DownloadCallCount(), Is.EqualTo(2));
            Assert.That(_clock.WaitCount, Is.EqualTo(0), "ended stream must not back off/relaunch");
        }

        [Test]
        public void should_give_up_after_max_consecutive_failures()
        {
            Mocker.GetMock<IConfigService>().Setup(c => c.YtDlpLiveMaxConsecutiveFailures).Returns(3);
            ScriptDownloads(Failed()); // always fails
            SetupProbe(ContentType.Live); // always still live

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.False);

            // 3 failures allowed, the 4th trips the budget (consecutiveFailures > max).
            Assert.That(DownloadCallCount(), Is.EqualTo(4));
        }

        [Test]
        public void should_stop_relaunching_when_cancelled()
        {
            // First attempt fails; cancel is requested during that attempt via the onStarted hook
            // so the loop sees cancellation before relaunching.
            ScriptDownloads(Failed());
            SetupProbe(ContentType.Live);

            _request.OnStarted = () => Subject.Cancel(_request.ContentId);

            var result = Subject.Supervise(_request);

            Assert.That(result.Success, Is.False);
            Assert.That(DownloadCallCount(), Is.EqualTo(1), "must not relaunch after cancellation");
        }

        [Test]
        public void should_report_supervising_state_during_run_and_clear_after()
        {
            ScriptDownloads(Merged());
            SetupProbe(ContentType.Vod);

            Assert.That(Subject.IsSupervising(_request.ContentId), Is.False);
            Subject.Supervise(_request);
            Assert.That(Subject.IsSupervising(_request.ContentId), Is.False);
        }

        // Controllable clock: Wait advances virtual time and counts calls so backoff is observable
        // without really sleeping.
        private sealed class TestClock : ILiveRetryClock
        {
            private DateTime _now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            public int WaitCount { get; private set; }

            public DateTime UtcNow => _now;

            public void Wait(TimeSpan duration, CancellationToken token)
            {
                WaitCount++;
                _now = _now.Add(duration);
            }
        }
    }
}
