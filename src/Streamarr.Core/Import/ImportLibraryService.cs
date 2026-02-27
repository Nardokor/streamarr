using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Core.Profiles.Qualities;

namespace Streamarr.Core.Import
{
    public class ImportLibraryResult
    {
        public int CreatorsCreated { get; set; }
        public int CreatorsMatched { get; set; }
        public int ChannelsCreated { get; set; }
        public int ContentLinked { get; set; }
        public int ContentAlreadyLinked { get; set; }
        public int FilesNotMatched { get; set; }
    }

    public interface IImportLibraryService
    {
        ImportLibraryResult Import(string rootPath, int? qualityProfileId = null);
    }

    public class ImportLibraryService : IImportLibraryService
    {
        private static readonly string[] VideoExtensions =
        {
            ".mp4", ".mkv", ".webm", ".avi", ".mov", ".m4v", ".ts", ".flv"
        };

        private static readonly Regex YouTubeIdRegex =
            new Regex(@"\[([a-zA-Z0-9_-]{11})\]", RegexOptions.Compiled);

        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IContentFileService _contentFileService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly IYouTubeApiClient _youTubeApiClient;
        private readonly Logger _logger;

        public ImportLibraryService(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IQualityProfileService qualityProfileService,
            IYouTubeApiClient youTubeApiClient,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _qualityProfileService = qualityProfileService;
            _youTubeApiClient = youTubeApiClient;
            _logger = logger;
        }

        public ImportLibraryResult Import(string rootPath, int? qualityProfileId = null)
        {
            var result = new ImportLibraryResult();

            if (!Directory.Exists(rootPath))
            {
                _logger.Warn("Import root path does not exist: {0}", rootPath);
                return result;
            }

            var profileId = ResolveQualityProfileId(qualityProfileId);

            foreach (var creatorDir in Directory.GetDirectories(rootPath))
            {
                ImportCreatorDirectory(creatorDir, rootPath, profileId, result);
            }

            _logger.Info(
                "Import complete — creators created: {0}, matched: {1}, channels created: {2}, " +
                "content linked: {3}, already linked: {4}, files not matched: {5}",
                result.CreatorsCreated,
                result.CreatorsMatched,
                result.ChannelsCreated,
                result.ContentLinked,
                result.ContentAlreadyLinked,
                result.FilesNotMatched);

            return result;
        }

        private int ResolveQualityProfileId(int? requested)
        {
            if (requested.HasValue && _qualityProfileService.Exists(requested.Value))
            {
                return requested.Value;
            }

            return _qualityProfileService.All().FirstOrDefault()?.Id ?? 1;
        }

        private void ImportCreatorDirectory(
            string dirPath,
            string rootPath,
            int qualityProfileId,
            ImportLibraryResult result)
        {
            var folderName = Path.GetFileName(dirPath);
            _logger.Debug("Importing creator directory: {0}", folderName);

            var creator = _creatorService.FindByTitle(folderName.CleanCreatorTitle());
            if (creator == null)
            {
                creator = _creatorService.AddCreator(new Creator
                {
                    Title = folderName,
                    Path = dirPath,
                    RootFolderPath = rootPath,
                    QualityProfileId = qualityProfileId,
                    Monitored = true,
                    Added = DateTime.UtcNow,
                });
                result.CreatorsCreated++;
                _logger.Debug("Created creator '{0}'", creator.Title);
            }
            else
            {
                result.CreatorsMatched++;
                _logger.Debug("Matched existing creator '{0}'", creator.Title);
            }

            var idToFile = ScanVideoFiles(dirPath);

            if (idToFile.Count == 0)
            {
                return;
            }

            var videos = _youTubeApiClient.GetVideoDetails(idToFile.Keys);
            var matchedIds = new HashSet<string>(videos.Select(v => v.Id));

            result.FilesNotMatched += idToFile.Keys.Count(id => !matchedIds.Contains(id));

            foreach (var video in videos)
            {
                if (!idToFile.TryGetValue(video.Id, out var filePath))
                {
                    continue;
                }

                ImportVideo(creator, video, filePath, result);
            }
        }

        private static Dictionary<string, string> ScanVideoFiles(string dirPath)
        {
            var idToFile = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(dirPath))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!VideoExtensions.Contains(ext))
                {
                    continue;
                }

                var videoId = ExtractYouTubeId(Path.GetFileName(file));
                if (videoId != null && !idToFile.ContainsKey(videoId))
                {
                    idToFile[videoId] = file;
                }
            }

            return idToFile;
        }

        private static string ExtractYouTubeId(string filename)
        {
            var match = YouTubeIdRegex.Match(filename);
            return match.Success ? match.Groups[1].Value : null;
        }

        private void ImportVideo(
            Creator creator,
            MetadataSource.YouTube.YoutubeVideo video,
            string filePath,
            ImportLibraryResult result)
        {
            var channelId = video.Snippet?.ChannelId;
            var channelTitle = video.Snippet?.ChannelTitle;

            if (string.IsNullOrWhiteSpace(channelId))
            {
                _logger.Warn(
                    "Video '{0}' has no channel ID in snippet; skipping",
                    video.Id);
                result.FilesNotMatched++;
                return;
            }

            var channel = FindOrCreateChannel(creator, channelId, channelTitle, result);
            var content = FindOrCreateContent(channel, video);

            if (content.ContentFileId > 0)
            {
                result.ContentAlreadyLinked++;
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var contentFile = _contentFileService.AddContentFile(new ContentFile
            {
                ContentId = content.Id,
                RelativePath = Path.GetFileName(filePath),
                Size = fileInfo.Length,
                DateAdded = DateTime.UtcNow,
                OriginalFilePath = filePath,
            });

            content.ContentFileId = contentFile.Id;
            content.Status = ContentStatus.Downloaded;
            _contentService.UpdateContent(content);

            result.ContentLinked++;
            _logger.Debug("Linked file '{0}' → content '{1}'", filePath, content.Title);
        }

        private Channel FindOrCreateChannel(
            Creator creator,
            string platformChannelId,
            string channelTitle,
            ImportLibraryResult result)
        {
            var channel = _channelService.FindByPlatformId(PlatformType.YouTube, platformChannelId);

            if (channel != null && channel.CreatorId != creator.Id)
            {
                // Channel belongs to a different creator — find one for this creator instead
                channel = _channelService
                    .GetByCreatorId(creator.Id)
                    .FirstOrDefault(c => c.PlatformId == platformChannelId);
            }

            if (channel != null)
            {
                return channel;
            }

            channel = _channelService.AddChannel(new Channel
            {
                CreatorId = creator.Id,
                Platform = PlatformType.YouTube,
                PlatformId = platformChannelId,
                PlatformUrl = $"https://www.youtube.com/channel/{platformChannelId}",
                Title = channelTitle ?? creator.Title,
                Monitored = true,
                DownloadVideos = true,
                DownloadShorts = true,
                DownloadLivestreams = true,
            });

            result.ChannelsCreated++;
            _logger.Debug("Created channel '{0}' (YouTube/{1})", channel.Title, platformChannelId);
            return channel;
        }

        private Content.Content FindOrCreateContent(Channel channel, MetadataSource.YouTube.YoutubeVideo video)
        {
            var existing = _contentService.FindByPlatformContentId(channel.Id, video.Id);
            if (existing != null)
            {
                return existing;
            }

            var content = _contentService.AddContent(new Content.Content
            {
                ChannelId = channel.Id,
                PlatformContentId = video.Id,
                ContentType = DetermineContentType(video),
                Title = video.Snippet?.Title ?? video.Id,
                Description = video.Snippet?.Description ?? string.Empty,
                ThumbnailUrl = video.Snippet?.Thumbnails?.Medium?.Url ?? string.Empty,
                Duration = ParseDuration(video.ContentDetails?.Duration),
                AirDateUtc = ParsePublishedAt(video.Snippet?.PublishedAt),
                DateAdded = DateTime.UtcNow,
                Monitored = true,
                Status = ContentStatus.Missing,
            });

            return content;
        }

        private static ContentType DetermineContentType(MetadataSource.YouTube.YoutubeVideo video)
        {
            if (video.LiveStreamingDetails != null)
            {
                return ContentType.Livestream;
            }

            var duration = ParseDuration(video.ContentDetails?.Duration);
            if (duration.HasValue && duration.Value.TotalMinutes < 3)
            {
                return ContentType.Short;
            }

            return ContentType.Video;
        }

        private static TimeSpan? ParseDuration(string isoDuration)
        {
            if (string.IsNullOrWhiteSpace(isoDuration))
            {
                return null;
            }

            try
            {
                return System.Xml.XmlConvert.ToTimeSpan(isoDuration);
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ParsePublishedAt(string publishedAt)
        {
            if (string.IsNullOrWhiteSpace(publishedAt))
            {
                return null;
            }

            if (DateTime.TryParse(
                    publishedAt,
                    null,
                    DateTimeStyles.RoundtripKind,
                    out var dt))
            {
                return dt.ToUniversalTime();
            }

            return null;
        }
    }
}
