using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Content.Commands
{
    public class CheckMirroredContentCommandExecutor : IExecute<CheckMirroredContentCommand>
    {
        private const int MaxIntervalDays = 64;

        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly Logger _logger;

        public CheckMirroredContentCommandExecutor(
            IChannelService channelService,
            IContentService contentService,
            IMetadataSourceFactory metadataSourceFactory,
            Logger logger)
        {
            _channelService = channelService;
            _contentService = contentService;
            _metadataSourceFactory = metadataSourceFactory;
            _logger = logger;
        }

        public void Execute(CheckMirroredContentCommand message)
        {
            var allWithFiles = _contentService.GetAllDownloadedOrMirrored()
                .Where(c => c.ContentFileId > 0)
                .GroupBy(c => c.ChannelId)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (allWithFiles.Count == 0)
            {
                return;
            }

            foreach (var channelId in allWithFiles.Keys)
            {
                var channel = _channelService.GetChannel(channelId);

                if (!IsDue(channel))
                {
                    continue;
                }

                try
                {
                    CheckChannel(channel, allWithFiles[channelId]);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unhandled error checking mirrored content for channel '{0}'", channel.Title);
                }
            }
        }

        private static bool IsDue(Channel channel)
        {
            if (channel.LastMirrorCheck == null)
            {
                return true;
            }

            return channel.LastMirrorCheck.Value.AddDays(channel.MirrorCheckIntervalDays) <= DateTime.UtcNow;
        }

        private void CheckChannel(Channel channel, List<Content> candidates)
        {
            var source = _metadataSourceFactory.GetByPlatform(channel.Platform);
            if (source == null)
            {
                return;
            }

            var ids = candidates.Select(c => c.PlatformContentId).ToList();

            HashSet<string> onPlatform;
            try
            {
                onPlatform = new HashSet<string>(
                    source.GetContentMetadataBatch(ids).Select(m => m.PlatformContentId),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to probe platform for Mirrored check on '{0}' — skipping", channel.Title);
                return;
            }

            var changed = 0;

            foreach (var content in candidates)
            {
                var stillOnPlatform = onPlatform.Contains(content.PlatformContentId);

                if (stillOnPlatform && content.Status == ContentStatus.Downloaded)
                {
                    content.Status = ContentStatus.Mirrored;
                    _contentService.UpdateContent(content);
                    changed++;
                }
                else if (!stillOnPlatform && content.Status == ContentStatus.Mirrored)
                {
                    content.Status = ContentStatus.Downloaded;
                    _contentService.UpdateContent(content);
                    changed++;
                }
            }

            // Double the interval while the channel is stable; reset to 1 day on any change.
            channel.LastMirrorCheck = DateTime.UtcNow;
            channel.MirrorCheckIntervalDays = changed == 0
                ? Math.Min(channel.MirrorCheckIntervalDays * 2, MaxIntervalDays)
                : 1;

            _channelService.UpdateChannel(channel);

            _logger.Debug(
                "Mirror check for '{0}': {1}/{2} item(s) changed, next check in {3} day(s)",
                channel.Title,
                changed,
                candidates.Count,
                channel.MirrorCheckIntervalDays);
        }
    }
}
