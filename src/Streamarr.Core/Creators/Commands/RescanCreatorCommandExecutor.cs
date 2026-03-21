using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Messaging.Commands;

namespace Streamarr.Core.Creators.Commands
{
    public class RescanCreatorCommandExecutor : IExecute<RescanCreatorCommand>
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
        private readonly Logger _logger;

        public RescanCreatorCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _logger = logger;
        }

        public void Execute(RescanCreatorCommand message)
        {
            var creators = message.CreatorId.HasValue
                ? new List<Creator> { _creatorService.GetCreator(message.CreatorId.Value) }
                : _creatorService.GetAllCreators();

            foreach (var creator in creators)
            {
                RescanCreator(creator);
            }
        }

        private void RescanCreator(Creator creator)
        {
            if (string.IsNullOrWhiteSpace(creator.Path) || !Directory.Exists(creator.Path))
            {
                _logger.Warn(
                    "Creator '{0}' path '{1}' does not exist; skipping rescan",
                    creator.Title,
                    creator.Path);
                return;
            }

            _logger.Info("Scanning local files for creator '{0}' at '{1}'", creator.Title, creator.Path);

            var idToFile = ScanVideoFiles(creator.Path);

            if (idToFile.Count == 0)
            {
                _logger.Info("No video files with IDs found for creator '{0}'", creator.Title);
                return;
            }

            _logger.Info("Found {0} video file(s) with IDs for '{1}'", idToFile.Count, creator.Title);

            var channels = _channelService.GetByCreatorId(creator.Id);
            var linked = 0;

            foreach (var channel in channels)
            {
                var contents = _contentService.GetByChannelId(channel.Id);

                foreach (var content in contents)
                {
                    if (content.ContentFileId > 0)
                    {
                        continue;
                    }

                    if (!idToFile.TryGetValue(content.PlatformContentId, out var filePath))
                    {
                        continue;
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

                    linked++;
                    _logger.Debug("Linked '{0}' to content '{1}'", filePath, content.Title);
                }
            }

            _logger.Info("Rescan complete for '{0}': {1} file(s) linked", creator.Title, linked);
        }

        private static Dictionary<string, string> ScanVideoFiles(string dirPath)
        {
            var idToFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(dirPath))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!VideoExtensions.Contains(ext))
                {
                    continue;
                }

                var match = YouTubeIdRegex.Match(Path.GetFileName(file));
                if (!match.Success)
                {
                    continue;
                }

                var videoId = match.Groups[1].Value;
                if (!idToFile.ContainsKey(videoId))
                {
                    idToFile[videoId] = file;
                }
            }

            return idToFile;
        }
    }
}
