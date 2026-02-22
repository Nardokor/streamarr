using System;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
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
        private readonly Logger _logger;

        public LivestreamStatusService(
            IContentService contentService,
            IYouTubeApiClient youTubeApiClient,
            Logger logger)
        {
            _contentService = contentService;
            _youTubeApiClient = youTubeApiClient;
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
                }
                else if (lsd.ActualStartTime.HasValue)
                {
                    // Stream has ended — revert to missing so it can be downloaded
                    newAirDate = lsd.ActualStartTime.Value;
                    if (content.Status == ContentStatus.Live)
                    {
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
