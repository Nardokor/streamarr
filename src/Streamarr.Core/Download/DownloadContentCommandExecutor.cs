using System;
using System.IO;
using System.Threading.Tasks;
using NLog;
using Streamarr.Common.Instrumentation.Extensions;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Download.YtDlp;
using Streamarr.Core.Extras;
using Streamarr.Core.History;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Notifications;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.Download
{
    public class DownloadContentCommandExecutor : IExecute<DownloadContentCommand>
    {
        private static readonly TimeSpan PostRecordingLiveCheckDelay = TimeSpan.FromMinutes(10);

        private readonly IContentService _contentService;
        private readonly IChannelService _channelService;
        private readonly ICreatorService _creatorService;
        private readonly IContentFileService _contentFileService;
        private readonly IYtDlpClient _ytDlpClient;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly INfoWriterService _nfoWriter;
        private readonly IDownloadHistoryService _historyService;
        private readonly ILivestreamStatusService _livestreamStatusService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DownloadContentCommandExecutor(IContentService contentService,
                                              IChannelService channelService,
                                              ICreatorService creatorService,
                                              IContentFileService contentFileService,
                                              IYtDlpClient ytDlpClient,
                                              IMetadataSourceFactory metadataSourceFactory,
                                              INfoWriterService nfoWriter,
                                              IDownloadHistoryService historyService,
                                              ILivestreamStatusService livestreamStatusService,
                                              IEventAggregator eventAggregator,
                                              Logger logger)
        {
            _contentService = contentService;
            _channelService = channelService;
            _creatorService = creatorService;
            _contentFileService = contentFileService;
            _ytDlpClient = ytDlpClient;
            _metadataSourceFactory = metadataSourceFactory;
            _nfoWriter = nfoWriter;
            _historyService = historyService;
            _livestreamStatusService = livestreamStatusService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Execute(DownloadContentCommand message)
        {
            var content = _contentService.GetContent(message.ContentId);
            var channel = _channelService.GetChannel(content.ChannelId);
            var creator = _creatorService.GetCreator(channel.CreatorId);

            var source = GetSource(channel.Platform);
            var url = source.GetDownloadUrl(content.PlatformContentId);

            _logger.ProgressInfo("Downloading '{0}' from {1}", content.Title, channel.Platform);

            var isLive = content.ContentType == ContentType.Live;

            // Preserve the pre-download status for error recovery. DownloadQueueStatusHandler
            // may not have run yet if a thread picked this up before the event fired.
            if (content.PreviousStatus == null)
            {
                content.PreviousStatus = content.Status;
            }

            // Show as Queued while waiting for a concurrent download slot.
            if (content.Status != ContentStatus.Queued)
            {
                content.Status = ContentStatus.Queued;
                _contentService.UpdateContent(content);
            }

            try
            {
                var result = _ytDlpClient.Download(
                    content.Id,
                    url,
                    creator.Path,
                    isLive,
                    source.CookiesFilePath,
                    progress =>
                    {
                        if (progress.PercentComplete.HasValue)
                        {
                            _logger.ProgressInfo("Downloading '{0}': {1:F1}%", content.Title, progress.PercentComplete.Value);
                        }
                    },
                    onStarted: () =>
                    {
                        // Slot acquired — transition to the active download state and fire notifications.
                        content.Status = isLive ? ContentStatus.Recording : ContentStatus.Downloading;
                        _contentService.UpdateContent(content);

                        if (!message.IsResume)
                        {
                            if (isLive)
                            {
                                _eventAggregator.PublishEvent(new LiveStreamStartedEvent
                                {
                                    Message = new LiveStreamStartedMessage
                                    {
                                        ContentTitle = content.Title,
                                        CreatorName = creator.Title,
                                        ChannelName = channel.Title,
                                    }
                                });
                            }
                            else
                            {
                                _eventAggregator.PublishEvent(new ContentGrabbedEvent
                                {
                                    Message = new ContentGrabbedMessage
                                    {
                                        ContentTitle = content.Title,
                                        CreatorName = creator.Title,
                                        ChannelName = channel.Title,
                                        ContentType = content.ContentType,
                                    }
                                });
                            }
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
                    content.PreviousStatus = null;

                    // A completed live recording is now a VOD — transition immediately
                    // rather than waiting for the next channel refresh.
                    if (isLive)
                    {
                        content.ContentType = ContentType.Vod;
                    }

                    _contentService.UpdateContent(content);

                    _nfoWriter.WriteCreatorNfo(creator);
                    _nfoWriter.WriteContentNfo(content, result.FilePath, channel);

                    _historyService.Record(
                        content.Id,
                        channel.Id,
                        creator.Id,
                        content.Title,
                        new QualityModel(),
                        DownloadHistoryEventType.Downloaded);

                    _eventAggregator.PublishEvent(new ContentDownloadedEvent
                    {
                        Message = new ContentDownloadedMessage
                        {
                            ContentTitle = content.Title,
                            CreatorName = creator.Title,
                            ChannelName = channel.Title,
                            ContentType = content.ContentType,
                            FileSize = result.FileSize
                        }
                    });

                    if (isLive)
                    {
                        _eventAggregator.PublishEvent(new LiveStreamEndedEvent
                        {
                            Message = new LiveStreamEndedMessage
                            {
                                ContentTitle = content.Title,
                                CreatorName = creator.Title,
                                ChannelName = channel.Title,
                                FileSize = result.FileSize,
                            }
                        });
                    }

                    _logger.Info("Successfully downloaded '{0}' ({1} bytes)", content.Title, result.FileSize);
                }
                else
                {
                    content.Status = content.PreviousStatus ?? ContentStatus.Missing;
                    content.PreviousStatus = null;
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
                content.Status = content.PreviousStatus ?? ContentStatus.Missing;
                content.PreviousStatus = null;
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

            if (isLive)
            {
                SchedulePostRecordingLiveCheck(channel);
            }
        }

        private void SchedulePostRecordingLiveCheck(Channel channel)
        {
            _logger.Debug(
                "Scheduling live re-check in {0} minutes for channel '{1}' after recording ended",
                (int)PostRecordingLiveCheckDelay.TotalMinutes,
                channel.Title);

            _ = Task.Run(async () =>
            {
                await Task.Delay(PostRecordingLiveCheckDelay);
                try
                {
                    _livestreamStatusService.RefreshLivestreamStatuses(channel);
                    _logger.Debug("Post-recording live re-check completed for '{0}'", channel.Title);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed post-recording live re-check for '{0}'", channel.Title);
                }
            });
        }

        private IMetadataSource GetSource(PlatformType platform)
        {
            return _metadataSourceFactory.GetByPlatform(platform)
                ?? throw new InvalidOperationException($"No enabled source configured for platform '{platform}'");
        }
    }
}
