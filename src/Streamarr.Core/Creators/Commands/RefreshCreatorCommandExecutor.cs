using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Import;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Creators.Commands
{
    public class RefreshCreatorCommandExecutor : IExecute<RefreshCreatorCommand>
    {
        // Limit membership probes for None/Unknown channels to this many per scheduled run.
        // Active channels are always probed (they have accessible content to monitor).
        private const int MaxMembershipChecksPerRun = 5;

        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IContentFileService _contentFileService;
        private readonly IContentFilterService _contentFilterService;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly ICreatorAvatarService _creatorAvatarService;
        private readonly ILivestreamStatusService _livestreamStatusService;
        private readonly IUnmatchedFileService _unmatchedFileService;
        private readonly Logger _logger;

        public RefreshCreatorCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IContentFilterService contentFilterService,
            IMetadataSourceFactory metadataSourceFactory,
            ICreatorAvatarService creatorAvatarService,
            ILivestreamStatusService livestreamStatusService,
            IUnmatchedFileService unmatchedFileService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _contentFilterService = contentFilterService;
            _metadataSourceFactory = metadataSourceFactory;
            _creatorAvatarService = creatorAvatarService;
            _livestreamStatusService = livestreamStatusService;
            _unmatchedFileService = unmatchedFileService;
            _logger = logger;
        }

        public void Execute(RefreshCreatorCommand message)
        {
            var creators = message.CreatorId.HasValue
                ? new List<Creator> { _creatorService.GetCreator(message.CreatorId.Value) }
                : _creatorService.GetMonitoredCreators();

            // Fetch channels once per creator to avoid redundant DB calls
            var creatorsWithChannels = creators
                .Select(c => (Creator: c, Channels: _channelService.GetByCreatorId(c.Id)))
                .ToList();

            // Avatar refresh: order doesn't matter
            foreach (var (creator, channels) in creatorsWithChannels)
            {
                try
                {
                    _logger.Info("Refreshing creator '{0}'", creator.Title);
                    RefreshAvatar(creator, channels);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unhandled error refreshing creator '{0}' — skipping to next", creator.Title);
                }
            }

            // Channel sync: sorted globally by LastMembershipCheck ascending (null = never = highest priority)
            // so that when YouTube rate-limits and the run is interrupted, the channels checked
            // least recently were already processed and nothing is permanently starved.
            var sortedChannels = creatorsWithChannels
                .SelectMany(x => x.Channels.Where(ch => ch.Monitored))
                .OrderBy(ch => ch.LastMembershipCheck ?? DateTime.MinValue)
                .ToList();

            var membershipChecksUsed = 0;
            var membershipRateLimited = false;

            foreach (var channel in sortedChannels)
            {
                try
                {
                    RefreshChannel(channel, ref membershipChecksUsed, ref membershipRateLimited);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unhandled error syncing channel '{0}' — skipping to next", channel.Title);
                }
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

        private void RefreshChannel(Channel channel, ref int membershipChecksUsed, ref bool membershipRateLimited)
        {
            _logger.Info("Syncing channel '{0}' ({1})", channel.Title, channel.Platform);

            var source = _metadataSourceFactory.GetByPlatform(channel.Platform);
            if (source == null)
            {
                _logger.Debug("Skipping unsupported platform: {0}", channel.Platform);
                return;
            }

            // Determine whether to probe the membership tab this sync.
            // Active: always probe (new members content may exist).
            // Unknown/None: probe up to MaxMembershipChecksPerRun times per run to avoid
            //   all bulk-imported channels checking simultaneously after their first week.
            // None: additionally requires the 7-day recheck threshold to be exceeded.
            var rateLimitedThisChannel = false;

            var membershipRecheckThreshold = TimeSpan.FromDays(30);
            var shouldCheckMembership = !membershipRateLimited && channel.Platform == PlatformType.YouTube &&
                channel.MembershipStatus switch
                {
                    MembershipStatus.Active  => true,
                    MembershipStatus.Unknown => membershipChecksUsed < MaxMembershipChecksPerRun,
                    MembershipStatus.None    => membershipChecksUsed < MaxMembershipChecksPerRun &&
                                               (channel.LastMembershipCheck == null ||
                                                DateTime.UtcNow - channel.LastMembershipCheck.Value > membershipRecheckThreshold),
                    _                        => false
                };

            // Gate the expensive per-video inaccessible re-probe separately.
            // For Active channels shouldCheckMembership is always true, so without this
            // the re-probe would fire on every sync — potentially hundreds of yt-dlp calls.
            var inaccessibleReprobeThreshold = TimeSpan.FromDays(30);
            var shouldReprobeInaccessible = shouldCheckMembership &&
                (channel.MembershipStatus != MembershipStatus.Active ||
                 channel.LastMembershipCheck == null ||
                 DateTime.UtcNow - channel.LastMembershipCheck.Value > inaccessibleReprobeThreshold);

            if (shouldCheckMembership && channel.MembershipStatus != MembershipStatus.Active)
            {
                membershipChecksUsed++;
            }

            _logger.Info(
                "Membership check decision for '{0}': status={1}, lastCheck={2}, shouldCheck={3}, shouldReprobe={4}",
                channel.Title,
                channel.MembershipStatus,
                channel.LastMembershipCheck?.ToString("u") ?? "never",
                shouldCheckMembership,
                shouldReprobeInaccessible);

            try
            {
                // Refresh channel category from platform metadata
                try
                {
                    var channelMeta = source.GetChannelMetadata(channel.PlatformUrl);
                    if (!string.IsNullOrEmpty(channelMeta.Category) && channelMeta.Category != channel.Category)
                    {
                        channel.Category = channelMeta.Category;
                        _channelService.UpdateChannel(channel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to refresh category for channel '{0}'", channel.Title);
                }

                // Pass null as since when the channel has no content yet so sources
                // do a full backfill rather than applying an incremental cutoff from
                // a previous sync that may have returned 0 items (e.g. due to a bug).
                var hasExistingContent = _contentService.GetByChannelId(channel.Id).Any();
                var sinceCutoff = hasExistingContent ? channel.LastInfoSync : null;
                var newItems = source.GetNewContent(channel.PlatformUrl, channel.PlatformId, sinceCutoff, shouldCheckMembership).ToList();

                _logger.Info(
                    "GetNewContent returned {0} item(s) for '{1}' ({2} members item(s))",
                    newItems.Count,
                    channel.Title,
                    newItems.Count(i => i.IsMembers));

                // If this source delegates livestream checks to another platform
                // (e.g. Fourthwall content IDs are YouTube IDs), batch-check the
                // delegate source now so items are stored with the correct ContentType
                // (Upcoming/Live) rather than always Video.
                var delegatePlatform = source.LivestreamDelegatePlatform;
                if (delegatePlatform != source.Platform && newItems.Any())
                {
                    var delegateSource = _metadataSourceFactory.GetByPlatform(delegatePlatform);
                    if (delegateSource != null)
                    {
                        try
                        {
                            var ids = newItems.Select(i => i.PlatformContentId).ToList();
                            var delegateMeta = delegateSource.GetContentMetadataBatch(ids)
                                .ToDictionary(m => m.PlatformContentId);

                            foreach (var item in newItems)
                            {
                                if (delegateMeta.TryGetValue(item.PlatformContentId, out var meta) &&
                                    (meta.ContentType == ContentType.Live || meta.ContentType == ContentType.Upcoming))
                                {
                                    item.ContentType = meta.ContentType;
                                    if (meta.AirDateUtc.HasValue)
                                    {
                                        item.AirDateUtc = meta.AirDateUtc;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Warn(ex, "Failed to batch-check livestream types via {0} delegate for '{1}'", delegatePlatform, channel.Title);
                        }
                    }
                }

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
                        // Covers both Live and Upcoming — the latter can get stuck if the race condition
                        // in LivestreamStatusService fired before the DB was updated to ContentType.Live.
                        if ((existing.ContentType == ContentType.Live || existing.ContentType == ContentType.Upcoming) && item.ContentType == ContentType.Vod)
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

                // Two-phase accessibility probe for new members content.
                // Phase 1: probe WITHOUT cookies (parallel) — YouTube always returns the
                //   required tier in the error, giving us tier info without needing access.
                // Phase 2: probe one video per unique tier WITH cookies — exit 0 = accessible.
                // This avoids mis-reading "members_only" availability as inaccessible and
                // keeps total probe count to (N videos) + (T unique tiers) instead of N.
                var newMembersContent = added.Where(c => c.IsMembers).ToList();
                if (newMembersContent.Any())
                {
                    _logger.Info("Discovering tiers for {0} new members video(s) (parallel, no cookies)...", newMembersContent.Count);

                    // Maps content ID → tier name (empty string = base membership, any level grants access)
                    var tierByContentId = new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    Parallel.ForEach(
                        newMembersContent,
                        new ParallelOptions { MaxDegreeOfParallelism = 4 },
                        content =>
                        {
                            var probe = source.ProbeContentAccessibility(content.PlatformContentId, withCookies: false);
                            if (!string.IsNullOrEmpty(probe.RequiredTier))
                            {
                                // Tier-specific content — requires that level or higher
                                tierByContentId[content.PlatformContentId] = probe.RequiredTier;
                                content.MembershipTier = probe.RequiredTier;
                            }
                            else if (probe.IsNotMember)
                            {
                                // "Join this channel" with no tier — accessible to any member (base level)
                                tierByContentId[content.PlatformContentId] = string.Empty;
                            }
                        });

                    // Phase 2: one probe per unique tier with cookies
                    var uniqueTiers = tierByContentId.Values
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    _logger.Info("{0} unique tier(s) found — probing with cookies...", uniqueTiers.Count);

                    var tierAccessible = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                    foreach (var tier in uniqueTiers)
                    {
                        var sampleId = tierByContentId
                            .First(kvp => string.Equals(kvp.Value, tier, StringComparison.OrdinalIgnoreCase))
                            .Key;
                        var probe = source.ProbeContentAccessibility(sampleId, withCookies: true);
                        if (probe.IsRateLimited)
                        {
                            _logger.Warn("Rate-limited by YouTube during membership probe for '{0}' — aborting membership check for this run", channel.Title);
                            rateLimitedThisChannel = true;
                            membershipRateLimited = true;
                            break;
                        }

                        tierAccessible[tier] = probe.IsAccessible;
                        _logger.Debug("Tier '{0}': {1}", string.IsNullOrEmpty(tier) ? "(base)" : tier, probe.IsAccessible ? "accessible" : "inaccessible");
                    }

                    // Phase 3: apply results — use tierByContentId so base-level videos
                    // (empty tier, no MembershipTier stored) are resolved correctly
                    var membershipConfirmedNew = false;
                    foreach (var content in newMembersContent)
                    {
                        var accessible = tierByContentId.TryGetValue(content.PlatformContentId, out var contentTier)
                            && tierAccessible.TryGetValue(contentTier, out var result)
                            && result;
                        content.IsAccessible = accessible;
                        if (accessible)
                        {
                            membershipConfirmedNew = true;
                        }
                        else
                        {
                            content.Status = ContentStatus.Unwanted;
                        }
                    }

                    _logger.Info(
                        "New members probe for '{0}': {1} tier(s), {2}",
                        channel.Title,
                        uniqueTiers.Count,
                        membershipConfirmedNew ? "membership confirmed" : "no accessible tiers found");
                }

                // Parallel probes for backfill items then persist
                if (backfillItems.Any())
                {
                    Parallel.ForEach(
                        backfillItems,
                        new ParallelOptions { MaxDegreeOfParallelism = 4 },
                        existing =>
                        {
                            var probe = source.ProbeContentAccessibility(existing.PlatformContentId);
                            existing.IsAccessible = probe.IsAccessible;
                            _logger.Debug("Backfilling members flag for '{0}' (accessible: {1})", existing.PlatformContentId, probe.IsAccessible);
                        });

                    foreach (var existing in backfillItems)
                    {
                        _contentService.UpdateContent(existing);
                    }
                }

                // Re-probe existing inaccessible members items when checking membership.
                // Handles the case where a user gains membership after content was first synced.
                // Gated on shouldReprobeInaccessible rather than shouldCheckMembership so that
                // Active channels (always shouldCheck=true) don't burn yt-dlp on every sync.
                if (shouldReprobeInaccessible)
                {
                    var allMembersContent = _contentService.GetByChannelId(channel.Id)
                        .Where(c => c.IsMembers)
                        .ToList();

                    if (allMembersContent.Any())
                    {
                        // Probe one video per unique tier across ALL members content (accessible and not).
                        // Covers both directions: gaining access (inaccessible → accessible) and
                        // losing access when membership lapses (accessible → inaccessible).
                        var tierGroups = allMembersContent
                            .GroupBy(c => c.MembershipTier ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        _logger.Info(
                            "Re-probing members content for '{0}': {1} video(s), {2} tier group(s)",
                            channel.Title,
                            allMembersContent.Count,
                            tierGroups.Count);

                        var tierResults = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                        var tierChangedIds = new System.Collections.Generic.HashSet<int>();

                        foreach (var group in tierGroups)
                        {
                            var storedTier = group.Key;
                            var tierLabel = string.IsNullOrEmpty(storedTier) ? "(base)" : storedTier;
                            var probe = source.ProbeContentAccessibility(group.First().PlatformContentId, withCookies: true);

                            if (probe.IsRateLimited)
                            {
                                _logger.Warn("Rate-limited by YouTube during re-probe for '{0}' — aborting membership check for this run", channel.Title);
                                rateLimitedThisChannel = true;
                                membershipRateLimited = true;
                                break;
                            }

                            tierResults[storedTier] = probe.IsAccessible;

                            if (!probe.IsAccessible && !string.IsNullOrEmpty(probe.RequiredTier) &&
                                !string.Equals(probe.RequiredTier, storedTier, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.Debug("Re-probe tier '{0}': changed to '{1}'", tierLabel, probe.RequiredTier);
                                foreach (var content in group)
                                {
                                    content.MembershipTier = probe.RequiredTier;
                                    tierChangedIds.Add(content.Id);
                                }
                            }
                            else
                            {
                                _logger.Debug("Re-probe tier '{0}': {1}", tierLabel, probe.IsAccessible ? "accessible" : "inaccessible");
                            }
                        }

                        foreach (var content in allMembersContent)
                        {
                            var tier = content.MembershipTier ?? string.Empty;
                            if (!tierResults.TryGetValue(tier, out var accessible))
                            {
                                continue;
                            }

                            var wasAccessible = content.IsAccessible;
                            content.IsAccessible = accessible;

                            if (accessible != wasAccessible || tierChangedIds.Contains(content.Id))
                            {
                                if (accessible)
                                {
                                    var passes = _contentFilterService.PassesFilter(content.Title, content.ContentType, channel, isMembers: true, isAccessible: true);
                                    content.Status = passes ? ContentStatus.Missing : ContentStatus.Unwanted;
                                }
                                else
                                {
                                    content.Status = ContentStatus.Unwanted;
                                }

                                _contentService.UpdateContent(content);
                            }
                        }

                        _logger.Info(
                            "Re-probe complete for '{0}': {1} probe(s), tiers: {2}",
                            channel.Title,
                            tierGroups.Count,
                            string.Join(", ", tierResults.Select(kv => $"{(string.IsNullOrEmpty(kv.Key) ? "(base)" : kv.Key)}={kv.Value}")));
                    }
                }

                if (added.Any())
                {
                    _logger.Info("Found {0} new item(s) for channel '{1}'", added.Count, channel.Title);
                    _contentService.AddContents(added);
                    ResolveUnmatchedFiles(channel, added);
                }

                // Re-evaluate filter for existing Missing/Unwanted items in case channel settings changed
                _contentFilterService.ReapplyFilterForChannel(channel);

                // Update membership status if we probed this sync and were not interrupted by rate-limiting.
                if (shouldCheckMembership && !rateLimitedThisChannel)
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

                    if (shouldReprobeInaccessible)
                    {
                        channel.LastMembershipCheck = DateTime.UtcNow;
                    }
                }
                else if (rateLimitedThisChannel)
                {
                    _logger.Info("Membership check for '{0}' aborted due to rate-limiting — LastMembershipCheck not updated", channel.Title);
                }

                channel.LastInfoSync = DateTime.UtcNow;
                _channelService.UpdateChannel(channel);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to sync channel '{0}'", channel.Title);

                // Record the re-probe attempt even on failure so a persistent sync error
                // doesn't cause the inaccessible re-probe to hammer channels every run.
                if (shouldReprobeInaccessible)
                {
                    channel.LastMembershipCheck = DateTime.UtcNow;
                    _channelService.UpdateChannel(channel);
                }
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

        private void ResolveUnmatchedFiles(Channel channel, List<Content.Content> newContent)
        {
            var unmatched = _unmatchedFileService.GetByCreatorId(channel.CreatorId);
            if (!unmatched.Any())
            {
                return;
            }

            foreach (var content in newContent)
            {
                var idToken = $"[{content.PlatformContentId}]";
                var match = unmatched.FirstOrDefault(u =>
                    u.FileName.Contains(idToken, StringComparison.OrdinalIgnoreCase));

                if (match == null)
                {
                    continue;
                }

                var fileInfo = new FileInfo(match.FilePath);
                var contentFile = _contentFileService.AddContentFile(new ContentFile
                {
                    ContentId = content.Id,
                    RelativePath = match.FileName,
                    Size = match.FileSize,
                    DateAdded = DateTime.UtcNow,
                    OriginalFilePath = match.FilePath,
                });

                content.ContentFileId = contentFile.Id;
                content.Status = ContentStatus.Downloaded;
                _contentService.UpdateContent(content);

                _unmatchedFileService.Delete(match.Id);

                _logger.Info(
                    "Resolved unmatched file '{0}' → content '{1}'",
                    match.FileName,
                    content.Title);
            }
        }
    }
}
