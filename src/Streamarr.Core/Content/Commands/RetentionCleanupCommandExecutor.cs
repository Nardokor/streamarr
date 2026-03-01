using System;
using System.IO;
using System.Linq;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Configuration;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Content.Commands
{
    public class RetentionCleanupCommandExecutor : IExecute<RetentionCleanupCommand>
    {
        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IContentFileService _contentFileService;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public RetentionCleanupCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IMetadataSourceFactory metadataSourceFactory,
            IDiskProvider diskProvider,
            IConfigService configService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _metadataSourceFactory = metadataSourceFactory;
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

                    var typeEligible = content.ContentType switch
                    {
                        ContentType.Video => channel.RetentionVideos,
                        ContentType.Short => channel.RetentionShorts,
                        ContentType.Vod   => channel.RetentionVods,
                        ContentType.Live  => channel.RetentionLive,
                        _                 => false
                    };

                    if (!typeEligible)
                    {
                        continue;
                    }

                    if (IsExemptByTitle(content.Title, channel.RetentionExceptionWords))
                    {
                        _logger.Debug("Content '{0}' is exempt from retention by exception words", content.Title);
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

        private static bool IsExemptByTitle(string title, string exceptionWords)
        {
            if (string.IsNullOrWhiteSpace(exceptionWords))
            {
                return false;
            }

            return exceptionWords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(w => title.Contains(w, StringComparison.OrdinalIgnoreCase));
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

            // Check current state on the platform
            var source = _metadataSourceFactory.GetByPlatform(channel.Platform);
            if (source != null)
            {
                ContentMetadataResult meta;
                try
                {
                    meta = source.GetContentMetadata(content.PlatformContentId);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to check platform for content '{0}', skipping", content.Title);
                    return;
                }

                if (meta == null)
                {
                    // Content gone from platform — preserve the file, mark as Deleted
                    _logger.Info("Content '{0}' is no longer on platform; keeping local file", content.Title);
                    content.Status = ContentStatus.Deleted;
                    _contentService.UpdateContent(content);
                    return;
                }

                // Check if duration shrank significantly (>5%) — indicates edited/re-uploaded content
                if (content.Duration.HasValue && meta.Duration.HasValue)
                {
                    var localSeconds = content.Duration.Value.TotalSeconds;

                    if (localSeconds > 0)
                    {
                        var shrinkRatio = (localSeconds - meta.Duration.Value.TotalSeconds) / localSeconds;
                        if (shrinkRatio > 0.05)
                        {
                            _logger.Info("Content '{0}' duration shrank by {1:P0}; marking as Modified", content.Title, shrinkRatio);
                            content.Status = ContentStatus.Modified;
                            _contentService.UpdateContent(content);
                            return;
                        }
                    }
                }
            }

            // Past retention, still on platform (or no source configured), unmodified — delete and mark Available
            var contentFile = _contentFileService.GetContentFile(content.ContentFileId);
            var fullPath = Path.Combine(creator.Path, contentFile.RelativePath);

            try
            {
                _diskProvider.DeleteFile(fullPath);
                _logger.Info("Deleted retained content file '{0}'", fullPath);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to delete file '{0}'", fullPath);
                return;
            }

            _contentFileService.DeleteContentFile(contentFile.Id);

            content.Status = ContentStatus.Available;
            content.ContentFileId = 0;
            _contentService.UpdateContent(content);
        }
    }
}
