using System;
using System.IO;
using NLog;
using Streamarr.Common.Instrumentation.Extensions;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.History;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.Download
{
    public class DownloadContentCommandExecutor : IExecute<DownloadContentCommand>
    {
        private readonly IContentService _contentService;
        private readonly IChannelService _channelService;
        private readonly ICreatorService _creatorService;
        private readonly IContentFileService _contentFileService;
        private readonly IYtDlpClient _ytDlpClient;
        private readonly IDownloadHistoryService _historyService;
        private readonly Logger _logger;

        public DownloadContentCommandExecutor(IContentService contentService,
                                              IChannelService channelService,
                                              ICreatorService creatorService,
                                              IContentFileService contentFileService,
                                              IYtDlpClient ytDlpClient,
                                              IDownloadHistoryService historyService,
                                              Logger logger)
        {
            _contentService = contentService;
            _channelService = channelService;
            _creatorService = creatorService;
            _contentFileService = contentFileService;
            _ytDlpClient = ytDlpClient;
            _historyService = historyService;
            _logger = logger;
        }

        public void Execute(DownloadContentCommand message)
        {
            var content = _contentService.GetContent(message.ContentId);
            var channel = _channelService.GetChannel(content.ChannelId);
            var creator = _creatorService.GetCreator(channel.CreatorId);

            var url = BuildDownloadUrl(channel.Platform, content.PlatformContentId);

            _logger.ProgressInfo("Downloading '{0}' from {1}", content.Title, channel.Platform);

            content.Status = ContentStatus.Downloading;
            _contentService.UpdateContent(content);

            try
            {
                var isLive = content.ContentType == ContentType.Livestream;
                var result = _ytDlpClient.Download(url, creator.Path, isLive, progress =>
                {
                    if (progress.PercentComplete.HasValue)
                    {
                        _logger.ProgressInfo("Downloading '{0}': {1:F1}%", content.Title, progress.PercentComplete.Value);
                    }
                });

                if (result.Success)
                {
                    var relativePath = Path.GetRelativePath(creator.Path, result.FilePath);

                    var contentFile = new ContentFile
                    {
                        ContentId = content.Id,
                        RelativePath = relativePath,
                        Size = result.FileSize,
                        DateAdded = DateTime.UtcNow,
                        OriginalFilePath = result.FilePath
                    };

                    contentFile = _contentFileService.AddContentFile(contentFile);

                    content.ContentFileId = contentFile.Id;
                    content.Status = ContentStatus.Downloaded;
                    _contentService.UpdateContent(content);

                    _historyService.Record(
                        content.Id,
                        channel.Id,
                        creator.Id,
                        content.Title,
                        new QualityModel(),
                        DownloadHistoryEventType.Downloaded);

                    _logger.Info("Successfully downloaded '{0}' ({1} bytes)", content.Title, result.FileSize);
                }
                else
                {
                    content.Status = ContentStatus.Missing;
                    _contentService.UpdateContent(content);

                    _historyService.Record(
                        content.Id,
                        channel.Id,
                        creator.Id,
                        content.Title,
                        new QualityModel(),
                        DownloadHistoryEventType.DownloadFailed,
                        result.ErrorMessage);

                    _logger.Error("Failed to download '{0}': {1} (exit code {2})", content.Title, result.ErrorMessage, result.ExitCode);
                }
            }
            catch (Exception ex)
            {
                content.Status = ContentStatus.Missing;
                _contentService.UpdateContent(content);

                _historyService.Record(
                    content.Id,
                    channel.Id,
                    creator.Id,
                    content.Title,
                    new QualityModel(),
                    DownloadHistoryEventType.DownloadFailed,
                    ex.Message);

                _logger.Error(ex, "Exception downloading '{0}'", content.Title);
                throw;
            }
        }

        private static string BuildDownloadUrl(PlatformType platform, string platformContentId)
        {
            return platform switch
            {
                PlatformType.YouTube => $"https://www.youtube.com/watch?v={platformContentId}",
                PlatformType.Twitch => $"https://www.twitch.tv/videos/{platformContentId}",
                _ => throw new ArgumentException($"Unsupported platform: {platform}")
            };
        }
    }
}
