using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            // User-set custom avatar always takes precedence over the platform thumbnail.
            if (!string.IsNullOrEmpty(creator.CustomThumbnailUrl))
            {
                return;
            }

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
                // Determine whether to probe the membership tab this sync.
                // Active: always probe (new members content may exist).
                // Unknown: always probe (first-time detection).
                // None: only probe if the last check was > 7 days ago (or never run).
                var membershipRecheckThreshold = TimeSpan.FromDays(7);
                var shouldCheckMembership = channel.MembershipStatus switch
                {
                    MembershipStatus.Active  => true,
                    MembershipStatus.Unknown => true,
                    MembershipStatus.None    => channel.LastMembershipCheck == null ||
                                               DateTime.UtcNow - channel.LastMembershipCheck.Value > membershipRecheckThreshold,
                    _                        => false
                };

                _logger.Info(
                    "Membership check decision for '{0}': status={1}, lastCheck={2}, shouldCheck={3}",
                    channel.Title,
                    channel.MembershipStatus,
                    channel.LastMembershipCheck?.ToString("u") ?? "never",
                    shouldCheckMembership);

                var newItems = source.GetNewContent(channel.PlatformUrl, channel.PlatformId, channel.LastInfoSync, shouldCheckMembership).ToList();

                _logger.Info(
                    "GetNewContent returned {0} item(s) for '{1}' ({2} members item(s))",
                    newItems.Count,
                    channel.Title,
                    newItems.Count(i => i.IsMembers));
                var added = new List<Content.Content>();
                var backfillItems = new List<Content.Content>();

                // First pass: DB lookups only — no probing yet
                foreach (var item in newItems)
                {
                    var existing = _contentService.FindByPlatformContentId(channel.Id, item.PlatformContentId);
                    if (existing != null)
                    {
                        // Backfill IsMembers for content that predates the members flag (e.g. migration 240).
                        if (item.IsMembers && !existing.IsMembers)
                        {
                            existing.IsMembers = true;
                            existing.IsAccessible = true; // resolved below
                            backfillItems.Add(existing);
                        }

                        // A recorded live stream that has since been archived on the platform becomes a VOD.
                        if (existing.ContentType == ContentType.Live && item.ContentType == ContentType.Vod)
                        {
                            existing.ContentType = ContentType.Vod;
                            _contentService.UpdateContent(existing);
                            _logger.Debug("Content '{0}' transitioned from Live to Vod after archiving", existing.Title);
                        }

                        continue;
                    }

                    // IsAccessible defaults to true; resolved in parallel below for members content
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

                // Parallel accessibility probes for new members content
                var newMembersContent = added.Where(c => c.IsMembers).ToList();
                if (newMembersContent.Any())
                {
                    _logger.Info("Probing accessibility for {0} new members video(s) (parallel)...", newMembersContent.Count);
                    Parallel.ForEach(
                        newMembersContent,
                        new ParallelOptions { MaxDegreeOfParallelism = 4 },
                        content =>
                        {
                            content.IsAccessible = source.ProbeContentAccessibility(content.PlatformContentId);
                            _logger.Debug("Members video '{0}': {1}", content.PlatformContentId, content.IsAccessible ? "accessible" : "inaccessible");
                            if (!content.IsAccessible)
                            {
                                content.Status = ContentStatus.Unwanted;
                            }
                        });
                }

                // Parallel probes for backfill items then persist
                if (backfillItems.Any())
                {
                    Parallel.ForEach(
                        backfillItems,
                        new ParallelOptions { MaxDegreeOfParallelism = 4 },
                        existing =>
                        {
                            existing.IsAccessible = source.ProbeContentAccessibility(existing.PlatformContentId);
                            _logger.Debug("Backfilling members flag for '{0}' (accessible: {1})", existing.PlatformContentId, existing.IsAccessible);
                        });

                    foreach (var existing in backfillItems)
                    {
                        _contentService.UpdateContent(existing);
                    }
                }

                // Re-probe existing inaccessible members items when checking membership.
                // Handles the case where a user gains membership after content was first synced.
                if (shouldCheckMembership)
                {
                    var existingInaccessible = _contentService.GetByChannelId(channel.Id)
                        .Where(c => c.IsMembers && !c.IsAccessible)
                        .ToList();

                    if (existingInaccessible.Any())
                    {
                        _logger.Info(
                            "Re-probing {0} previously inaccessible members video(s) for '{1}'",
                            existingInaccessible.Count,
                            channel.Title);

                        Parallel.ForEach(
                            existingInaccessible,
                            new ParallelOptions { MaxDegreeOfParallelism = 4 },
                            content =>
                            {
                                content.IsAccessible = source.ProbeContentAccessibility(content.PlatformContentId);
                                _logger.Debug(
                                    "Re-probe '{0}': {1}",
                                    content.PlatformContentId,
                                    content.IsAccessible ? "now accessible" : "still inaccessible");
                            });

                        foreach (var content in existingInaccessible.Where(c => c.IsAccessible))
                        {
                            var passes = _contentFilterService.PassesFilter(content.Title, content.ContentType, channel, isMembers: true, isAccessible: true);
                            content.Status = passes ? ContentStatus.Missing : ContentStatus.Unwanted;
                            _contentService.UpdateContent(content);
                        }
                    }
                }

                if (added.Any())
                {
                    _logger.Info("Found {0} new item(s) for channel '{1}'", added.Count, channel.Title);
                    _contentService.AddContents(added);
                }

                // Re-evaluate filter for existing Missing/Unwanted items in case channel settings changed
                _contentFilterService.ReapplyFilterForChannel(channel);

                // Update membership status if we probed the tab this sync.
                if (shouldCheckMembership)
                {
                    // Re-query the full channel content so we account for both newly-added
                    // items and previously-existing accessible members content. Using only
                    // `added` misses the case where all members content was already in the
                    // DB from a prior sync (added would be empty → status wrongly set to None).
                    var allChannelContent = _contentService.GetByChannelId(channel.Id);
                    var hasAccessibleMembersContent = allChannelContent.Any(c => c.IsMembers && c.IsAccessible);
                    var newMembershipStatus = hasAccessibleMembersContent
                        ? MembershipStatus.Active
                        : MembershipStatus.None;

                    _logger.Info(
                        "Membership probe result for '{0}': accessible members content exists={1} → status {2} → {3}",
                        channel.Title,
                        hasAccessibleMembersContent,
                        channel.MembershipStatus,
                        newMembershipStatus);

                    channel.MembershipStatus = newMembershipStatus;
                    channel.LastMembershipCheck = DateTime.UtcNow;
                }

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
