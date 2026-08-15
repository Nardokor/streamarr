#nullable enable
using System;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource;

namespace Streamarr.Core.Import
{
    public enum UrlImportStatus
    {
        Started,
        AlreadyDownloaded,
        NeedsTarget,
        Error
    }

    public class UrlImportResult
    {
        public UrlImportStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ContentId { get; set; }
        public int? CreatorId { get; set; }
        public string CreatorTitle { get; set; } = string.Empty;
        public int? ChannelId { get; set; }
        public string ResolvedTitle { get; set; } = string.Empty;
        public string ResolvedChannelTitle { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
    }

    public interface IUrlImportService
    {
        UrlImportResult Import(string url, int? channelId);
    }

    public class UrlImportService : IUrlImportService
    {
        private readonly IChannelService _channelService;
        private readonly ICreatorService _creatorService;
        private readonly IContentService _contentService;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public UrlImportService(
            IChannelService channelService,
            ICreatorService creatorService,
            IContentService contentService,
            IMetadataSourceFactory metadataSourceFactory,
            IManageCommandQueue commandQueueManager,
            Logger logger)
        {
            _channelService = channelService;
            _creatorService = creatorService;
            _contentService = contentService;
            _metadataSourceFactory = metadataSourceFactory;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        public UrlImportResult Import(string url, int? channelId)
        {
            var platform = DetectPlatform(url);
            if (platform == null)
            {
                return Error("Couldn't determine the platform for this URL.");
            }

            var source = _metadataSourceFactory.GetByPlatform(platform.Value);
            if (source == null)
            {
                return Error($"No metadata source configured for {platform}.");
            }

            var meta = source.ResolveFromUrl(url);
            if (meta == null)
            {
                return Error("Couldn't resolve this URL — it may be invalid, deleted, or unsupported.");
            }

            Channel channel;
            if (channelId.HasValue)
            {
                channel = _channelService.GetChannel(channelId.Value);
            }
            else
            {
                var matched = _channelService.FindByPlatformId(platform.Value, meta.PlatformChannelId);
                if (matched == null)
                {
                    return new UrlImportResult
                    {
                        Status = UrlImportStatus.NeedsTarget,
                        Message = "No existing creator matches this channel — pick a target to import into.",
                        ResolvedTitle = meta.Title,
                        ResolvedChannelTitle = meta.PlatformChannelTitle,
                        ThumbnailUrl = meta.ThumbnailUrl
                    };
                }

                channel = matched;
            }

            var creator = _creatorService.GetCreator(channel.CreatorId);

            var content = _contentService.FindByPlatformContentId(channel.Id, meta.PlatformContentId);
            if (content != null)
            {
                var alreadyOnDisk = content.ContentFileId > 0 ||
                                    content.Status == ContentStatus.Downloaded;

                if (alreadyOnDisk)
                {
                    return new UrlImportResult
                    {
                        Status = UrlImportStatus.AlreadyDownloaded,
                        Message = $"'{content.Title}' is already downloaded.",
                        ContentId = content.Id,
                        CreatorId = creator.Id,
                        CreatorTitle = creator.Title,
                        ChannelId = channel.Id,
                        ResolvedTitle = content.Title
                    };
                }
            }
            else
            {
                content = _contentService.AddContent(new Content.Content
                {
                    ChannelId = channel.Id,
                    PlatformContentId = meta.PlatformContentId,
                    ContentType = meta.ContentType,
                    Title = meta.Title ?? meta.PlatformContentId,
                    Description = meta.Description ?? string.Empty,
                    ThumbnailUrl = meta.ThumbnailUrl ?? string.Empty,
                    Duration = meta.Duration,
                    AirDateUtc = meta.AirDateUtc,
                    DateAdded = DateTime.UtcNow,
                    Monitored = true,
                    IsMembers = meta.IsMembers,
                    IsAccessible = meta.IsAccessible,
                    Status = ContentStatus.Missing
                });
            }

            _commandQueueManager.Push(new DownloadContentCommand { ContentId = content.Id });

            _logger.Info("Queued manual URL import '{0}' for creator '{1}'", content.Title, creator.Title);

            return new UrlImportResult
            {
                Status = UrlImportStatus.Started,
                Message = $"Queued '{content.Title}' for '{creator.Title}'.",
                ContentId = content.Id,
                CreatorId = creator.Id,
                CreatorTitle = creator.Title,
                ChannelId = channel.Id,
                ResolvedTitle = content.Title
            };
        }

        private static UrlImportResult Error(string message)
        {
            return new UrlImportResult { Status = UrlImportStatus.Error, Message = message };
        }

        private static PlatformType? DetectPlatform(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var host = uri.Host.ToLowerInvariant();

            if (host.Contains("youtube.com") || host.Contains("youtu.be"))
            {
                return PlatformType.YouTube;
            }

            if (host.Contains("twitch.tv"))
            {
                return PlatformType.Twitch;
            }

            if (host.Contains("fansly.com"))
            {
                return PlatformType.Fansly;
            }

            if (host.Contains("party.gg"))
            {
                return PlatformType.Party;
            }

            if (host.Contains("patreon.com"))
            {
                return PlatformType.Patreon;
            }

            if (host.Contains("twitter.com") || host.Contains("x.com"))
            {
                return PlatformType.Twitter;
            }

            return null;
        }
    }
}
