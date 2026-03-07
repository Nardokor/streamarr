using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Creators.Commands
{
    public class RefreshCreatorCommandExecutor : IExecute<RefreshCreatorCommand>
    {
        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IContentFilterService _contentFilterService;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly ICreatorAvatarService _creatorAvatarService;
        private readonly ILivestreamStatusService _livestreamStatusService;
        private readonly Logger _logger;

        public RefreshCreatorCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFilterService contentFilterService,
            IMetadataSourceFactory metadataSourceFactory,
            ICreatorAvatarService creatorAvatarService,
            ILivestreamStatusService livestreamStatusService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFilterService = contentFilterService;
            _metadataSourceFactory = metadataSourceFactory;
            _creatorAvatarService = creatorAvatarService;
            _livestreamStatusService = livestreamStatusService;
            _logger = logger;
        }

        public void Execute(RefreshCreatorCommand message)
        {
            var creators = message.CreatorId.HasValue
                ? new List<Creator> { _creatorService.GetCreator(message.CreatorId.Value) }
                : _creatorService.GetMonitoredCreators();

            foreach (var creator in creators)
            {
                RefreshCreator(creator);
            }
        }

        private void RefreshCreator(Creator creator)
        {
            _logger.Info("Refreshing creator '{0}'", creator.Title);

            var channels = _channelService.GetByCreatorId(creator.Id);

            RefreshAvatar(creator, channels);

            foreach (var channel in channels.Where(c => c.Monitored))
            {
                RefreshChannel(channel);
            }
        }

        private void RefreshAvatar(Creator creator, List<Channel> channels)
        {
            // Use the first channel for which we have a configured source
            foreach (var channel in channels)
            {
                var source = _metadataSourceFactory.GetByPlatform(channel.Platform);
                if (source == null)
                {
                    continue;
                }

                try
                {
                    var channelMeta = source.GetChannelMetadata(channel.PlatformUrl);
                    if (!string.IsNullOrEmpty(channelMeta.ThumbnailUrl))
                    {
                        creator.ThumbnailUrl = channelMeta.ThumbnailUrl;
                        _creatorAvatarService.DownloadAvatar(creator);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to refresh avatar for creator '{0}'", creator.Title);
                }

                break; // Only need one channel for avatar
            }
        }

        private void RefreshChannel(Channel channel)
        {
            _logger.Info("Syncing channel '{0}' ({1})", channel.Title, channel.Platform);

            var source = _metadataSourceFactory.GetByPlatform(channel.Platform);
            if (source == null)
            {
                _logger.Debug("Skipping unsupported platform: {0}", channel.Platform);
                return;
            }

            try
            {
                var newItems = source.GetNewContent(channel.PlatformUrl, channel.PlatformId, channel.LastInfoSync);
                var added = new List<Content.Content>();

                foreach (var item in newItems)
                {
                    var existing = _contentService.FindByPlatformContentId(channel.Id, item.PlatformContentId);
                    if (existing != null)
                    {
                        // Backfill IsMembers for content that predates the members flag (e.g. migration 240).
                        if (item.IsMembers && !existing.IsMembers)
                        {
                            existing.IsMembers = true;
                            existing.IsAccessible = source.ProbeContentAccessibility(item.PlatformContentId);
                            _logger.Debug(
                                "Backfilling members flag for '{0}' (accessible: {1})",
                                item.PlatformContentId,
                                existing.IsAccessible);
                            _contentService.UpdateContent(existing);
                        }

                        continue;
                    }

                    // For newly discovered members-only content, probe accessibility once.
                    if (item.IsMembers)
                    {
                        item.IsAccessible = source.ProbeContentAccessibility(item.PlatformContentId);
                        _logger.Debug(
                            "Members video '{0}' accessibility: {1}",
                            item.PlatformContentId,
                            item.IsAccessible ? "accessible" : "inaccessible");
                    }

                    var passes = _contentFilterService.PassesFilter(item.Title, item.ContentType, channel, item.IsMembers, item.IsAccessible);

                    added.Add(new Content.Content
                    {
                        ChannelId = channel.Id,
                        PlatformContentId = item.PlatformContentId,
                        ContentType = item.ContentType,
                        Title = item.Title,
                        Description = item.Description,
                        ThumbnailUrl = item.ThumbnailUrl,
                        Duration = item.Duration,
                        AirDateUtc = item.AirDateUtc,
                        DateAdded = DateTime.UtcNow,
                        Monitored = true,
                        IsMembers = item.IsMembers,
                        IsAccessible = item.IsAccessible,
                        Status = passes ? ContentStatus.Missing : ContentStatus.Unwanted
                    });
                }

                if (added.Any())
                {
                    _logger.Info("Found {0} new item(s) for channel '{1}'", added.Count, channel.Title);
                    _contentService.AddContents(added);
                }

                // Re-evaluate filter for existing Missing/Unwanted items in case channel settings changed
                _contentFilterService.ReapplyFilterForChannel(channel);

                channel.LastInfoSync = DateTime.UtcNow;
                _channelService.UpdateChannel(channel);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to sync channel '{0}'", channel.Title);
            }

            // Live check runs independently so a playlist scan failure doesn't block it.
            try
            {
                _livestreamStatusService.RefreshLivestreamStatuses(channel);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to check livestream status for channel '{0}'", channel.Title);
            }
        }
    }
}
