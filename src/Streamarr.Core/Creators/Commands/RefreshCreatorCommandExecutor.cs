using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.YouTube;

namespace Streamarr.Core.Creators.Commands
{
    public class RefreshCreatorCommandExecutor : IExecute<RefreshCreatorCommand>
    {
        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly YouTubeMetadataService _youTubeMetadataService;
        private readonly Logger _logger;

        public RefreshCreatorCommandExecutor(ICreatorService creatorService,
                                             IChannelService channelService,
                                             IContentService contentService,
                                             IManageCommandQueue commandQueueManager,
                                             YouTubeMetadataService youTubeMetadataService,
                                             Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _commandQueueManager = commandQueueManager;
            _youTubeMetadataService = youTubeMetadataService;
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

            foreach (var channel in channels.Where(c => c.Monitored))
            {
                RefreshChannel(channel);
            }
        }

        private void RefreshChannel(Channel channel)
        {
            _logger.Info("Syncing channel '{0}' ({1})", channel.Title, channel.Platform);

            try
            {
                List<ContentMetadataResult> newItems;

                if (channel.Platform == PlatformType.YouTube)
                {
                    newItems = _youTubeMetadataService.GetNewContent(channel.PlatformUrl, channel.LastInfoSync);
                }
                else
                {
                    _logger.Debug("Skipping unsupported platform: {0}", channel.Platform);
                    return;
                }

                var added = new List<Content.Content>();

                foreach (var item in newItems)
                {
                    if (_contentService.FindByPlatformContentId(channel.Id, item.PlatformContentId) != null)
                    {
                        continue;
                    }

                    // Content type filter
                    var typeAllowed = item.ContentType switch
                    {
                        ContentType.Video      => channel.DownloadVideos,
                        ContentType.Short      => channel.DownloadShorts,
                        ContentType.Livestream => channel.DownloadLivestreams,
                        _                      => true
                    };

                    if (!typeAllowed)
                    {
                        continue;
                    }

                    // Title keyword filter (OR logic, case-insensitive)
                    if (!string.IsNullOrWhiteSpace(channel.TitleFilter))
                    {
                        var terms = channel.TitleFilter.Split(
                            new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var lower = (item.Title ?? string.Empty).ToLowerInvariant();

                        if (!terms.Any(t => lower.Contains(t.ToLowerInvariant())))
                        {
                            continue;
                        }
                    }

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
                        Status = ContentStatus.Missing
                    });
                }

                if (added.Any())
                {
                    _logger.Info("Found {0} new item(s) for channel '{1}'", added.Count, channel.Title);
                    _contentService.AddContents(added);
                }

                var missing = _contentService.GetMissingContent(channel.Id);
                var downloadCommands = missing
                    .Where(c => c.Monitored)
                    .Select(c => new DownloadContentCommand { ContentId = c.Id })
                    .ToList();

                if (downloadCommands.Any())
                {
                    _logger.Debug("Queuing {0} download(s) for channel '{1}'", downloadCommands.Count, channel.Title);
                    _commandQueueManager.PushMany(downloadCommands);
                }

                channel.LastInfoSync = DateTime.UtcNow;
                _channelService.UpdateChannel(channel);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to sync channel '{0}'", channel.Title);
            }
        }
    }
}
