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

            var livestreamContent = existing
                .Where(c => (c.ContentType == ContentType.VoD ||
                             c.ContentType == ContentType.Live ||
                             c.ContentType == ContentType.Upcoming) &&
                            c.Status != ContentStatus.Downloaded &&
                            c.Status != ContentStatus.Deleted)
                .ToList();

            if (!livestreamContent.Any())
            {
                return;
            }

            _logger.Debug(
                "Re-fetching statuses for {0} stream item(s) in '{1}'",
                livestreamContent.Count,
                channel.Title);

            var videoDetails = _youTubeApiClient.GetVideoDetails(livestreamContent.Select(c => c.PlatformContentId));

            foreach (var video in videoDetails)
            {
                var content = livestreamContent.FirstOrDefault(c => c.PlatformContentId == video.Id);
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
                ContentType? newContentType = null;
                ContentStatus? newStatus = null;

                if (lsd.ScheduledStartTime.HasValue && lsd.ScheduledStartTime.Value > DateTime.UtcNow)
                {
                    // Still upcoming — not yet started
                    newAirDate = lsd.ScheduledStartTime.Value;
                    newContentType = ContentType.Upcoming;
                }
                else if (lsd.ActualStartTime.HasValue && !lsd.ActualEndTime.HasValue)
                {
                    // Currently live
                    newAirDate = lsd.ActualStartTime.Value;
                    newContentType = ContentType.Live;
                    newStatus = ContentStatus.Recording;

                    // If RecordLiveOnly: queue a download to capture the stream as it airs
                    if (channel.RecordLiveOnly && content.Status != ContentStatus.Recording)
                    {
                        _logger.Debug(
                            "Queuing live recording for '{0}' (RecordLiveOnly)",
                            content.Title);

                        _commandQueueManager.Push(new DownloadContentCommand { ContentId = content.Id });
                    }
                }
                else if (lsd.ActualStartTime.HasValue)
                {
                    // Stream has ended — now an archived VoD
                    newAirDate = lsd.ActualStartTime.Value;
                    newContentType = ContentType.VoD;

                    if (channel.RecordLiveOnly)
                    {
                        // RecordLiveOnly: don't queue the archive download
                        _logger.Debug(
                            "Stream '{0}' ended; skipping archive download (RecordLiveOnly)",
                            content.Title);
                    }
                    else if (content.Status == ContentStatus.Recording)
                    {
                        // Normal mode: revert to Missing so the archive VoD can be downloaded
                        newStatus = ContentStatus.Missing;
                    }
                }

                var changed = false;

                if (newAirDate.HasValue && newAirDate != content.AirDateUtc)
                {
                    content.AirDateUtc = newAirDate;
                    changed = true;
                }

                if (newContentType.HasValue && newContentType != content.ContentType)
                {
                    content.ContentType = newContentType.Value;
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
                        "Updated stream '{0}': ContentType={1}, AirDateUtc={2}, Status={3}",
                        content.Title,
                        content.ContentType,
                        content.AirDateUtc,
                        content.Status);
                }
            }
        }
    }
}
