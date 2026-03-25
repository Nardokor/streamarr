using System;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;
using ContentEntity = Streamarr.Core.Content.Content;

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
                // No known live/upcoming content — if the channel wants live streams,
                // probe the channel directly so a new stream is discovered and can
                // start recording without waiting for the next full sync.
                if (channel.DownloadLive)
                {
                    TryDiscoverActiveLivestream(channel, source);
                }

                return;
            }

            _logger.Debug(
                "Re-fetching statuses for {0} stream item(s) in '{1}'",
                livestreamContent.Count,
                channel.Title);

            var updates = source
                .GetLivestreamStatusUpdates(livestreamContent.Select(c => c.PlatformContentId))
                .ToDictionary(u => u.PlatformContentId);

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

                ContentStatus? newStatus = null;

                if (update.NewContentType == ContentType.Live)
                {
                    if (channel.DownloadLive)
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

        private void TryDiscoverActiveLivestream(Channel channel, IMetadataSource source)
        {
            var live = source.GetActiveLivestream(channel.PlatformUrl, channel.PlatformId);
            if (live == null)
            {
                return;
            }

            // Skip if the content already exists — a recent full sync may have added it.
            var existing = _contentService.FindByPlatformContentId(channel.Id, live.PlatformContentId);
            if (existing != null)
            {
                return;
            }

            _logger.Info(
                "Discovered active livestream '{0}' ({1}) for '{2}' via live check",
                live.Title,
                live.PlatformContentId,
                channel.Title);

            var passes = _contentFilterService.PassesFilter(live.Title, ContentType.Live, channel);
            var content = new ContentEntity
            {
                ChannelId = channel.Id,
                PlatformContentId = live.PlatformContentId,
                ContentType = ContentType.Live,
                Title = live.Title,
                ThumbnailUrl = live.ThumbnailUrl,
                AirDateUtc = live.AirDateUtc,
                DateAdded = DateTime.UtcNow,
                Monitored = true,
                Status = passes ? ContentStatus.Missing : ContentStatus.Unwanted,
            };

            var added = _contentService.AddContent(content);

            if (passes && channel.AutoDownload)
            {
                added.Status = ContentStatus.Recording;
                _contentService.UpdateContent(added);
                _commandQueueManager.Push(new DownloadContentCommand { ContentId = added.Id });
                _logger.Info("Queued live recording for '{0}' (auto-discovered via live check)", live.Title);
            }
        }
    }
}
