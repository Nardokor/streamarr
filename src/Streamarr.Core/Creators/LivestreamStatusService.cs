using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Creators
{
    public interface ILivestreamStatusService
    {
        void RefreshLivestreamStatuses(Channel channel);
    }

    public class LivestreamStatusService : ILivestreamStatusService
    {
        private readonly IContentService _contentService;
        private readonly IContentFilterService _contentFilterService;
        private readonly MetadataSourceFactory _metadataSourceFactory;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public LivestreamStatusService(
            IContentService contentService,
            IContentFilterService contentFilterService,
            MetadataSourceFactory metadataSourceFactory,
            IManageCommandQueue commandQueueManager,
            Logger logger)
        {
            _contentService = contentService;
            _contentFilterService = contentFilterService;
            _metadataSourceFactory = metadataSourceFactory;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public void RefreshLivestreamStatuses(Channel channel)
        {
            var channelSource = _metadataSourceFactory.GetByPlatform(channel.Platform);
            if (channelSource == null)
            {
                return;
            }

            // If the channel's content IDs belong to a different platform (e.g. Fourthwall
            // hosts unlisted YouTube videos), use that platform's source for status checks.
            var delegatePlatform = channelSource.LivestreamDelegatePlatform;
            var source = delegatePlatform != channel.Platform
                ? (_metadataSourceFactory.GetByPlatform(delegatePlatform) ?? channelSource)
                : channelSource;

            UpdateTrackedStatuses(channel, source);
        }

        private void UpdateTrackedStatuses(Channel channel, IMetadataSource source)
        {
            var existing = _contentService.GetByChannelId(channel.Id);

            // Only probe Live and Upcoming items — VODs have already transitioned and
            // their LiveStreamingDetails never change, so including them would burn
            // one videos.list call per 50 undownloaded VODs on every live-check cycle.
            var livestreamContent = existing
                .Where(c => (c.ContentType == ContentType.Live ||
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

            var updates = source
                .GetLivestreamStatusUpdates(livestreamContent.Select(c => c.PlatformContentId))
                .ToDictionary(u => u.PlatformContentId);

            _logger.Debug(
                "Got {0} update(s) from API for {1} tracked item(s) in '{2}'",
                updates.Count,
                livestreamContent.Count,
                channel.Title);

            foreach (var content in livestreamContent)
            {
                if (!updates.TryGetValue(content.PlatformContentId, out var update))
                {
                    // Video is absent from the API response — it was deleted or removed while live.
                    // Transition it out of the live/upcoming state so it is no longer probed.
                    _logger.Info(
                        "Live content '{0}' ({1}) is no longer available on the platform; marking as Unwanted",
                        content.Title,
                        content.PlatformContentId);

                    content.ContentType = ContentType.Vod;
                    content.Status = ContentStatus.Unwanted;
                    _contentService.UpdateContent(content);
                    continue;
                }

                _logger.Debug(
                    "'{0}' ({1}): DB={2}/{3} → API={4}",
                    content.Title,
                    content.PlatformContentId,
                    content.ContentType,
                    content.Status,
                    update.NewContentType);

                ContentStatus? newStatus = null;

                if (update.NewContentType == ContentType.Live)
                {
                    if (!channel.DownloadLive)
                    {
                        _logger.Debug(
                            "Stream '{0}' is live but DownloadLive=false; tracking only",
                            content.Title);
                    }
                    else
                    {
                        if (content.Status == ContentStatus.Unwanted)
                        {
                            _logger.Debug(
                                "Stream '{0}' is live but filtered (Unwanted); skipping recording",
                                content.Title);
                        }
                        else if (channel.AutoDownload)
                        {
                            newStatus = ContentStatus.Recording;

                            if (content.Status != ContentStatus.Recording)
                            {
                                _logger.Debug(
                                    "Queuing live recording for '{0}' (DownloadLive)",
                                    content.Title);

                                _commandQueueManager.Push(new DownloadContentCommand { ContentId = content.Id });
                            }
                        }
                        else
                        {
                            _logger.Debug(
                                "Stream '{0}' is live but AutoDownload=false; recording suppressed",
                                content.Title);
                        }
                    }
                }
                else if (update.NewContentType == ContentType.Vod)
                {
                    if (content.Status == ContentStatus.Recording)
                    {
                        newStatus = channel.DownloadVods ? ContentStatus.Missing : ContentStatus.Unwanted;
                    }
                    else if (content.Status == ContentStatus.Missing || content.Status == ContentStatus.Unwanted)
                    {
                        // If this is a live sentinel (no real VOD ID yet), keep it Unwanted.
                        // The archived VOD will appear as a separate item with a real ID on the next sync.
                        if (content.PlatformContentId.StartsWith("live:"))
                        {
                            newStatus = ContentStatus.Unwanted;
                            _logger.Debug(
                                "Stream '{0}' ended without being recorded; archived VOD will appear on next sync",
                                content.Title);
                        }
                        else
                        {
                            var passes = _contentFilterService.PassesFilter(content.Title, ContentType.Vod, channel);
                            newStatus = passes ? ContentStatus.Missing : ContentStatus.Unwanted;

                            if (!channel.DownloadVods)
                            {
                                _logger.Debug(
                                    "Stream '{0}' ended; skipping archive download (DownloadVods=false)",
                                    content.Title);
                            }
                        }
                    }
                }

                var changed = false;

                if (update.NewAirDateUtc.HasValue && update.NewAirDateUtc != content.AirDateUtc)
                {
                    content.AirDateUtc = update.NewAirDateUtc;
                    changed = true;
                }

                if (update.NewContentType != content.ContentType)
                {
                    content.ContentType = update.NewContentType;
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
