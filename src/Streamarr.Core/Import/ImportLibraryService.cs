using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.Extras;
using Streamarr.Core.MetadataSource;
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

    public class ImportableFolder
    {
        public string FolderName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public interface IImportLibraryService
    {
        List<ImportableFolder> GetImportableFolders(string rootPath);
        ImportLibraryResult Import(string rootPath, IEnumerable<string> folderNames);
    }

    public class ImportLibraryService : IImportLibraryService
    {
        private static readonly string[] VideoExtensions =
        {
            ".mp4", ".mkv", ".webm", ".avi", ".mov", ".m4v", ".ts", ".flv"
        };

        // Matches the 11-char YouTube ID in brackets: [xxxxxxxxxxx]
        private static readonly Regex YouTubeIdRegex =
            new Regex(@"\[([a-zA-Z0-9_-]{11})\]", RegexOptions.Compiled);

        private readonly ICreatorService _creatorService;
        private readonly IChannelService _channelService;
        private readonly IContentService _contentService;
        private readonly IContentFileService _contentFileService;
        private readonly IQualityProfileService _qualityProfileService;
        private readonly IMetadataSourceFactory _metadataSourceFactory;
        private readonly INfoWriterService _nfoWriter;
        private readonly IUnmatchedFileService _unmatchedFileService;
        private readonly Logger _logger;

        public ImportLibraryService(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IQualityProfileService qualityProfileService,
            IMetadataSourceFactory metadataSourceFactory,
            INfoWriterService nfoWriter,
            IUnmatchedFileService unmatchedFileService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _qualityProfileService = qualityProfileService;
            _metadataSourceFactory = metadataSourceFactory;
            _nfoWriter = nfoWriter;
            _unmatchedFileService = unmatchedFileService;
            _logger = logger;
        }

        public List<ImportableFolder> GetImportableFolders(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                return new List<ImportableFolder>();
            }

            var result = new List<ImportableFolder>();

            foreach (var dirPath in Directory.GetDirectories(rootPath).OrderBy(d => d))
            {
                if (!_creatorService.CreatorPathExists(dirPath))
                {
                    result.Add(new ImportableFolder
                    {
                        FolderName = System.IO.Path.GetFileName(dirPath),
                        Path = dirPath,
                    });
                }
            }

            return result;
        }

        public ImportLibraryResult Import(string rootPath, IEnumerable<string> folderNames)
        {
            var result = new ImportLibraryResult();

            if (!Directory.Exists(rootPath))
            {
                _logger.Warn("Import root path does not exist: {0}", rootPath);
                return result;
            }

            var profileId = ResolveQualityProfileId(null);

            foreach (var folderName in folderNames)
            {
                var creatorDir = System.IO.Path.Combine(rootPath, folderName);
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

            _nfoWriter.WriteCreatorNfo(creator);

            var (idToFile, noIdFiles) = ScanVideoFiles(dirPath);

            foreach (var filePath in noIdFiles)
            {
                RecordUnmatched(creator.Id, filePath, UnmatchedFileReason.NoYouTubeId);
                result.FilesNotMatched++;
            }

            if (idToFile.Count == 0)
            {
                return;
            }

            // For now, import is YouTube-only (files contain YouTube IDs in their names)
            var source = _metadataSourceFactory.GetByPlatform(PlatformType.YouTube);
            if (source == null)
            {
                _logger.Warn("No YouTube metadata source configured; cannot import files in '{0}'", dirPath);
                foreach (var filePath in idToFile.Values)
                {
                    RecordUnmatched(creator.Id, filePath, UnmatchedFileReason.NoMetadataSource);
                }

                result.FilesNotMatched += idToFile.Count;
                return;
            }

            var defaults = (MetadataSourceSettingsBase)source.Definition.Settings;
            var metas = source.GetContentMetadataBatch(idToFile.Keys).ToList();
            var matchedIds = new HashSet<string>(metas.Select(m => m.PlatformContentId));

            foreach (var id in idToFile.Keys.Where(id => !matchedIds.Contains(id)))
            {
                RecordUnmatched(creator.Id, idToFile[id], UnmatchedFileReason.MetadataNotFound);
                result.FilesNotMatched++;
            }

            foreach (var meta in metas)
            {
                if (!idToFile.TryGetValue(meta.PlatformContentId, out var filePath))
                {
                    continue;
                }

                ImportVideo(creator, meta, filePath, defaults, result);
            }
        }

        private static (Dictionary<string, string> IdToFile, List<string> NoIdFiles) ScanVideoFiles(string dirPath)
        {
            var idToFile = new Dictionary<string, string>();
            var noIdFiles = new List<string>();

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
                else if (videoId == null)
                {
                    noIdFiles.Add(file);
                }
            }

            return (idToFile, noIdFiles);
        }

        private static string ExtractYouTubeId(string filename)
        {
            var match = YouTubeIdRegex.Match(filename);
            return match.Success ? match.Groups[1].Value : null;
        }

        private void ImportVideo(
            Creator creator,
            ContentMetadataResult meta,
            string filePath,
            MetadataSourceSettingsBase defaults,
            ImportLibraryResult result)
        {
            if (string.IsNullOrWhiteSpace(meta.PlatformChannelId))
            {
                _logger.Warn("Content '{0}' has no channel ID; skipping", meta.PlatformContentId);
                RecordUnmatched(creator.Id, filePath, UnmatchedFileReason.NoChannelId);
                result.FilesNotMatched++;
                return;
            }

            var channel = FindOrCreateChannel(creator, meta.PlatformChannelId, meta.PlatformChannelTitle, defaults, result);
            var content = FindOrCreateContent(channel, meta);

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

            _nfoWriter.WriteContentNfo(content, filePath, channel);

            result.ContentLinked++;
            _logger.Debug("Linked file '{0}' → content '{1}'", filePath, content.Title);
        }

        private Channel FindOrCreateChannel(
            Creator creator,
            string platformChannelId,
            string channelTitle,
            MetadataSourceSettingsBase defaults,
            ImportLibraryResult result)
        {
            var channel = _channelService.FindByPlatformId(PlatformType.YouTube, platformChannelId);

            if (channel != null && channel.CreatorId != creator.Id)
            {
                channel = _channelService
                    .GetByCreatorId(creator.Id)
                    .FirstOrDefault(c => c.PlatformId == platformChannelId);
            }

            if (channel != null)
            {
                return channel;
            }

            var newChannel = new Channel
            {
                CreatorId = creator.Id,
                Platform = PlatformType.YouTube,
                PlatformId = platformChannelId,
                PlatformUrl = $"https://www.youtube.com/channel/{platformChannelId}",
                Title = channelTitle ?? creator.Title,
                Monitored = true,
            };

            defaults.ApplyDefaultsTo(newChannel);

            channel = _channelService.AddChannel(newChannel, creator.Title);

            result.ChannelsCreated++;
            _logger.Debug("Created channel '{0}' (YouTube/{1})", channel.Title, platformChannelId);
            return channel;
        }

        private void RecordUnmatched(int creatorId, string filePath, UnmatchedFileReason reason)
        {
            var fileInfo = new FileInfo(filePath);

            // Use LastWriteTimeUtc (mtime) so the date reflects the file's original age,
            // not when the import ran. mtime is preserved by cp, rsync, and yt-dlp.
            var fileDate = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.UtcNow;

            _unmatchedFileService.Add(new UnmatchedFile
            {
                CreatorId = creatorId,
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                DateFound = fileDate,
                Reason = reason,
            });
        }

        private Content.Content FindOrCreateContent(Channel channel, ContentMetadataResult meta)
        {
            var existing = _contentService.FindByPlatformContentId(channel.Id, meta.PlatformContentId);
            if (existing != null)
            {
                return existing;
            }

            return _contentService.AddContent(new Content.Content
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
                Status = ContentStatus.Missing,
            });
        }
    }
}
