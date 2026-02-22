using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Configuration;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource.YouTube;

namespace Streamarr.Core.Content.Commands
{
    public class RetentionCleanupCommandExecutor : IExecute<RetentionCleanupCommand>
    {
        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IContentFileService _contentFileService;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public RetentionCleanupCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IYouTubeApiClient youTubeApiClient,
            IDiskProvider diskProvider,
            IConfigService configService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _youTubeApiClient = youTubeApiClient;
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public void Execute(RetentionCleanupCommand message)
        {
            var allDownloaded = _contentService.GetAllDownloaded();

            var byChannel = allDownloaded.GroupBy(c => c.ChannelId);

            foreach (var group in byChannel)
            {
                var channel = _channelService.GetChannel(group.Key);
                var effectiveRetention = channel.RetentionDays ?? _configService.DefaultRetentionDays;

                if (effectiveRetention <= 0)
                {
                    continue;
                }

                var creator = _creatorService.GetCreator(channel.CreatorId);
                var cutoff = DateTime.UtcNow.AddDays(-effectiveRetention);

                foreach (var content in group)
                {
                    if (!content.AirDateUtc.HasValue || content.AirDateUtc.Value > cutoff)
                    {
                        continue;
                    }

                    try
                    {
                        ProcessExpiredContent(content, channel, creator);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to process retention for content '{0}'", content.Title);
                    }
                }
            }
        }

        private void ProcessExpiredContent(Content content, Channel channel, Creator creator)
        {
            _logger.Debug("Checking retention for content '{0}' (aired {1:d})", content.Title, content.AirDateUtc);

            if (content.ContentFileId == 0)
            {
                content.Status = ContentStatus.Expired;
                _contentService.UpdateContent(content);
                return;
            }

            // Check current state on the platform (YouTube only for now)
            if (channel.Platform == PlatformType.YouTube)
            {
                List<YoutubeVideo> details;
                try
                {
                    details = _youTubeApiClient.GetVideoDetails(new[] { content.PlatformContentId });
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to check YouTube for content '{0}', skipping", content.Title);
                    return;
                }

                var video = details.FirstOrDefault();

                if (video == null)
                {
                    // Video gone from YouTube — preserve the file, mark as Deleted
                    _logger.Info("Content '{0}' is no longer on YouTube; keeping local file", content.Title);
                    content.Status = ContentStatus.Deleted;
                    _contentService.UpdateContent(content);
                    return;
                }

                // Check if duration shrank significantly (>5%) — indicates edited/re-uploaded content
                if (content.Duration.HasValue && !string.IsNullOrEmpty(video.ContentDetails?.Duration))
                {
                    try
                    {
                        var apiDuration = System.Xml.XmlConvert.ToTimeSpan(video.ContentDetails.Duration);
                        var localSeconds = content.Duration.Value.TotalSeconds;

                        if (localSeconds > 0)
                        {
                            var shrinkRatio = (localSeconds - apiDuration.TotalSeconds) / localSeconds;
                            if (shrinkRatio > 0.05)
                            {
                                _logger.Info("Content '{0}' duration shrank by {1:P0}; marking as Modified", content.Title, shrinkRatio);
                                content.Status = ContentStatus.Modified;
                                _contentService.UpdateContent(content);
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to parse API duration for content '{0}'", content.Title);
                    }
                }
            }

            // Past retention with no special condition — delete the file and mark Expired
            var contentFile = _contentFileService.GetContentFile(content.ContentFileId);
            var fullPath = Path.Combine(creator.Path, contentFile.RelativePath);

            try
            {
                _diskProvider.DeleteFile(fullPath);
                _logger.Info("Deleted expired content file '{0}'", fullPath);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to delete file '{0}'", fullPath);
                return;
            }

            _contentFileService.DeleteContentFile(contentFile.Id);

            content.Status = ContentStatus.Expired;
            content.ContentFileId = 0;
            _contentService.UpdateContent(content);
        }
    }
}
