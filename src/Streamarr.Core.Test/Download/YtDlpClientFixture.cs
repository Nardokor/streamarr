using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Streamarr.Common.Processes;
using Streamarr.Core.Configuration;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.Test.Framework;

namespace Streamarr.Core.Test.Download
{
    [TestFixture]
    public class YtDlpClientFixture : CoreTest<YtDlpClient>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(c => c.YtDlpBinaryPath)
                  .Returns("yt-dlp");

            Mocker.GetMock<IConfigService>()
                  .Setup(c => c.YtDlpPreferredFormat)
                  .Returns("bestvideo+bestaudio/best");
        }

        private static ProcessOutput OkOutput(params string[] lines)
        {
            var output = new ProcessOutput { ExitCode = 0 };
            output.Lines.AddRange(lines.Select(l => new ProcessOutputLine(ProcessOutputLevel.Standard, l)));
            return output;
        }

        private static ProcessOutput ErrorOutput(int exitCode = 1, string error = "error")
        {
            var output = new ProcessOutput { ExitCode = exitCode };
            output.Lines.Add(new ProcessOutputLine(ProcessOutputLevel.Error, error));
            return output;
        }

        // ── IsAvailable ───────────────────────────────────────────────────────

        [Test]
        public void is_available_should_return_true_when_version_string_returned()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", "--version", null))
                  .Returns(OkOutput("2024.01.01"));

            Subject.IsAvailable().Should().BeTrue();
        }

        [Test]
        public void is_available_should_return_false_when_process_throws()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", "--version", null))
                  .Throws(new Exception("yt-dlp not found"));

            Subject.IsAvailable().Should().BeFalse();
        }

        [Test]
        public void is_available_should_return_false_when_exit_code_is_nonzero()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", "--version", null))
                  .Returns(ErrorOutput());

            Subject.IsAvailable().Should().BeFalse();
        }

        // ── GetVersion ────────────────────────────────────────────────────────

        [Test]
        public void get_version_should_return_trimmed_first_line()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", "--version", null))
                  .Returns(OkOutput("  2024.01.01  "));

            Subject.GetVersion().Should().Be("2024.01.01");
        }

        [Test]
        public void get_version_should_throw_when_exit_code_is_nonzero()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", "--version", null))
                  .Returns(ErrorOutput(1));

            Action act = () => Subject.GetVersion();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*yt-dlp*");
        }

        // ── GetChannelInfo ────────────────────────────────────────────────────

        [Test]
        public void get_channel_info_should_deserialize_json_from_stdout()
        {
            var json = "{\"channel\":\"Test Channel\",\"channel_id\":\"UCtest123\",\"channel_url\":\"https://youtube.com/c/test\",\"description\":\"\",\"thumbnail\":\"\",\"uploader\":\"\",\"uploader_id\":\"\",\"uploader_url\":\"\",\"entries\":[]}";

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Returns(OkOutput(json));

            var result = Subject.GetChannelInfo("https://www.youtube.com/@test");

            result.Channel.Should().Be("Test Channel");
            result.ChannelId.Should().Be("UCtest123");
        }

        [Test]
        public void get_channel_info_should_throw_when_exit_code_is_nonzero()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Returns(ErrorOutput(1, "Unable to extract channel info"));

            Action act = () => Subject.GetChannelInfo("https://www.youtube.com/@missing");

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*channel info*");
        }

        // ── GetVideoInfo ──────────────────────────────────────────────────────

        [Test]
        public void get_video_info_should_deserialize_json_from_stdout()
        {
            var json = "{\"id\":\"abc123\",\"title\":\"Test Video\",\"description\":\"desc\",\"thumbnail\":\"\",\"upload_date\":\"20240101\",\"channel\":\"Test\",\"channel_id\":\"UCtest\",\"channel_url\":\"\",\"uploader_url\":\"\",\"webpage_url\":\"\"}";

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Returns(OkOutput(json));

            var result = Subject.GetVideoInfo("https://www.youtube.com/watch?v=abc123");

            result.Id.Should().Be("abc123");
            result.Title.Should().Be("Test Video");
        }

        [Test]
        public void get_video_info_should_throw_when_exit_code_is_nonzero()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Returns(ErrorOutput(1, "Video unavailable"));

            Action act = () => Subject.GetVideoInfo("https://www.youtube.com/watch?v=gone");

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*video info*");
        }

        // ── GetChannelVideos ──────────────────────────────────────────────────

        [Test]
        public void get_channel_videos_should_aggregate_results_across_all_three_tabs()
        {
            var video1Json = "{\"id\":\"vid1\",\"title\":\"Video 1\",\"description\":\"\",\"thumbnail\":\"\",\"upload_date\":\"20240101\",\"channel\":\"\",\"channel_id\":\"\",\"channel_url\":\"\",\"uploader_url\":\"\",\"webpage_url\":\"\"}";
            var short1Json = "{\"id\":\"short1\",\"title\":\"Short 1\",\"description\":\"\",\"thumbnail\":\"\",\"upload_date\":\"20240101\",\"channel\":\"\",\"channel_id\":\"\",\"channel_url\":\"\",\"uploader_url\":\"\",\"webpage_url\":\"\"}";
            var stream1Json = "{\"id\":\"stream1\",\"title\":\"Stream 1\",\"description\":\"\",\"thumbnail\":\"\",\"upload_date\":\"20240101\",\"channel\":\"\",\"channel_id\":\"\",\"channel_url\":\"\",\"uploader_url\":\"\",\"webpage_url\":\"\"}";

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.Is<string>(a => a.Contains("/videos")), null))
                  .Returns(OkOutput(video1Json));

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.Is<string>(a => a.Contains("/shorts")), null))
                  .Returns(OkOutput(short1Json));

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.Is<string>(a => a.Contains("/streams")), null))
                  .Returns(OkOutput(stream1Json));

            var result = Subject.GetChannelVideos("https://www.youtube.com/c/test");

            result.Should().HaveCount(3);
            result.Select(v => v.Id).Should().Contain(new[] { "vid1", "short1", "stream1" });
        }

        [Test]
        public void get_channel_videos_should_deduplicate_items_that_appear_in_multiple_tabs()
        {
            var sharedJson = "{\"id\":\"shared1\",\"title\":\"Shared\",\"description\":\"\",\"thumbnail\":\"\",\"upload_date\":\"20240101\",\"channel\":\"\",\"channel_id\":\"\",\"channel_url\":\"\",\"uploader_url\":\"\",\"webpage_url\":\"\"}";

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Returns(OkOutput(sharedJson));

            var result = Subject.GetChannelVideos("https://www.youtube.com/c/test");

            result.Should().HaveCount(1);
            result[0].Id.Should().Be("shared1");
        }

        [Test]
        public void get_channel_videos_should_strip_existing_tab_suffix_before_appending()
        {
            var capturedArgs = new List<string>();

            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Callback<string, string, System.Collections.Specialized.StringDictionary>(
                      (_, args, __) => capturedArgs.Add(args))
                  .Returns(OkOutput());

            Subject.GetChannelVideos("https://www.youtube.com/c/test/videos");

            capturedArgs.Should().NotContain(a => a.Contains("/videos/videos"),
                "the existing /videos suffix should be stripped before new tabs are appended");
        }

        [Test]
        public void get_channel_videos_should_return_empty_list_when_all_tabs_fail()
        {
            Mocker.GetMock<IProcessProvider>()
                  .Setup(p => p.StartAndCapture("yt-dlp", It.IsAny<string>(), null))
                  .Returns(ErrorOutput(1, "tab unavailable"));

            var result = Subject.GetChannelVideos("https://www.youtube.com/c/test");

            result.Should().BeEmpty();
        }
    }
}
