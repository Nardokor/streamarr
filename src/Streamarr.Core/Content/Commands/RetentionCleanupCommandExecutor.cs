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
using Streamarr.Core.RootFolders;

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
        private readonly IRootFolderService _rootFolderService;
        private readonly Logger _logger;

        public RetentionCleanupCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IMetadataSourceFactory metadataSourceFactory,
            IDiskProvider diskProvider,
            IConfigService configService,
            IRootFolderService rootFolderService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _metadataSourceFactory = metadataSourceFactory;
            _diskProvider = diskProvider;
            _configService = configService;
            _rootFolderService = rootFolderService;
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

                    var alwaysKeep = content.ContentType switch
                    {
                        ContentType.Video => content.IsMembers ? channel.KeepMembers : channel.KeepVideos,
                        ContentType.Short => content.IsMembers ? channel.KeepMembers : channel.KeepShorts,
                        ContentType.Vod   => content.IsMembers ? channel.KeepMembers : channel.KeepVods,
                        ContentType.Live  => content.IsMembers && channel.KeepMembers,
                        _                 => true
                    };

                    if (alwaysKeep)
                    {
                        continue;
                    }

                    if (MatchesKeepWords(content.Title, channel.RetentionKeepWords))
                    {
                        _logger.Debug("Content '{0}' is preserved from retention by keep words", content.Title);
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

        private static bool MatchesKeepWords(string title, string keepWords)
        {
            var terms = ParseTerms(keepWords);
            if (terms.Length == 0)
            {
                return false;
            }

            var lower = (title ?? string.Empty).ToLowerInvariant();
            return terms.Any(w => lower.Contains(w));
        }

        private static string[] ParseTerms(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            return input
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 0)
                .ToArray();
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

            // Past retention, still on platform (or no source configured), unmodified — move to recycle bin
            var contentFile = _contentFileService.GetContentFile(content.ContentFileId);
            var fullPath = Path.Combine(creator.Path, contentFile.RelativePath);
            var rootFolderPath = _rootFolderService.GetBestRootFolderPath(creator.Path);
            var recycleBinPath = Path.Combine(rootFolderPath, ".recycle");

            try
            {
                _diskProvider.MoveToRecycleBin(fullPath, recycleBinPath);
                _logger.Info("Moved retained content file '{0}' to recycle bin", fullPath);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to move file '{0}' to recycle bin", fullPath);
                return;
            }

            _contentFileService.DeleteContentFile(contentFile.Id);

            content.Status = ContentStatus.Available;
            content.ContentFileId = 0;
            _contentService.UpdateContent(content);
        }
    }
}
