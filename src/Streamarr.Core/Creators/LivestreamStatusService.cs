using System;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource.YouTube;

namespace Streamarr.Core.Creators
{
    public interface ILivestreamStatusService
    {
        void RefreshLivestreamStatuses(Channel channel);
    }

    public class LivestreamStatusService : ILivestreamStatusService
    {
        private readonly IContentService _contentService;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public LivestreamStatusService(
            IContentService contentService,
            IYouTubeApiClient youTubeApiClient,
            IManageCommandQueue commandQueueManager,
            Logger logger)
        {
            _contentService = contentService;
            _youTubeApiClient = youTubeApiClient;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public void RefreshLivestreamStatuses(Channel channel)
        {
            var existing = _contentService.GetByChannelId(channel.Id);

            var upcoming = existing
                .Where(c => c.ContentType == ContentType.Livestream &&
                            c.Status != ContentStatus.Downloaded &&
                            c.Status != ContentStatus.Deleted)
                .ToList();

            if (!upcoming.Any())
            {
                return;
            }

            _logger.Debug(
                "Re-fetching air dates for {0} upcoming/live livestream(s) in '{1}'",
                upcoming.Count,
                channel.Title);

            var videoDetails = _youTubeApiClient.GetVideoDetails(upcoming.Select(c => c.PlatformContentId));

            foreach (var video in videoDetails)
            {
                var content = upcoming.FirstOrDefault(c => c.PlatformContentId == video.Id);
                if (content == null)
                {
                    continue;
                }

                var lsd = video.LiveStreamingDetails;
                if (lsd == null)
                {
                    continue;
                }

                DateTime? newAirDate = null;
                ContentStatus? newStatus = null;

                if (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow)
                {
                    // Still upcoming
                    newAirDate = lsd.ScheduledStartTime.Value;
                }
                else if (lsd.ActualStartTime.HasValue && !lsd.ActualEndTime.HasValue)
                {
                    // Stream has started but not ended — currently live
                    newAirDate = lsd.ActualStartTime.Value;
                    newStatus = ContentStatus.Live;

                    // If RecordLiveOnly: queue a download to capture the stream as it airs
                    if (channel.RecordLiveOnly && content.Status != ContentStatus.Live)
                    {
                        _logger.Debug(
                            "Queuing live recording for '{0}' (RecordLiveOnly)",
                            content.Title);

                        _commandQueueManager.Push(new DownloadContentCommand { ContentId = content.Id });
                    }
                }
                else if (lsd.ActualStartTime.HasValue)
                {
                    // Stream has ended
                    newAirDate = lsd.ActualStartTime.Value;

                    if (channel.RecordLiveOnly)
                    {
                        // RecordLiveOnly: don't transition to Missing — the archive VOD is ignored
                        _logger.Debug(
                            "Stream '{0}' ended; skipping archive download (RecordLiveOnly)",
                            content.Title);
                    }
                    else if (content.Status == ContentStatus.Live)
                    {
                        // Normal mode: revert to Missing so the archive VOD can be downloaded
                        newStatus = ContentStatus.Missing;
                    }
                }

                var changed = false;

                if (newAirDate.HasValue && newAirDate != content.AirDateUtc)
                {
                    content.AirDateUtc = newAirDate;
                    changed = true;
                }

                if (newStatus.HasValue && newStatus != content.Status)
                {
                    content.Status = newStatus.Value;
                    changed = true;
                }

                if (changed)
                {
                    _contentService.UpdateContent(content);
                    _logger.Debug(
                        "Updated livestream '{0}': AirDateUtc={1}, Status={2}",
                        content.Title,
                        content.AirDateUtc,
                        content.Status);
                }
            }
        }
    }
}
