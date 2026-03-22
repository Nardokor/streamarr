using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Import;
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
        private readonly IUnmatchedFileService _unmatchedFileService;
        private readonly Logger _logger;

        public RescanCreatorCommandExecutor(
            ICreatorService creatorService,
            IChannelService channelService,
            IContentService contentService,
            IContentFileService contentFileService,
            IUnmatchedFileService unmatchedFileService,
            Logger logger)
        {
            _creatorService = creatorService;
            _channelService = channelService;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _unmatchedFileService = unmatchedFileService;
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

            var channels = _channelService.GetByCreatorId(creator.Id);

            // Prune stale unmatched records before scanning so the fresh state is accurate
            PruneStaleUnmatched(creator, channels);

            ScanVideoFiles(creator.Path, out var idToFile, out var noIdFiles);

            // Reload after pruning so existingUnmatchedPaths reflects the cleaned state
            var existingUnmatchedPaths = new HashSet<string>(
                _unmatchedFileService.GetByCreatorId(creator.Id).Select(u => u.FilePath),
                StringComparer.OrdinalIgnoreCase);

            // Files with no recognisable ID go to unmatched (skip if already recorded)
            foreach (var filePath in noIdFiles)
            {
                if (existingUnmatchedPaths.Contains(filePath))
                {
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                _unmatchedFileService.Add(new UnmatchedFile
                {
                    CreatorId = creator.Id,
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileSize = fileInfo.Length,
                    DateFound = fileInfo.LastWriteTimeUtc,
                    Reason = UnmatchedFileReason.NoYouTubeId,
                });
            }

            if (idToFile.Count == 0)
            {
                _logger.Info("No video files with IDs found for creator '{0}'", creator.Title);
                return;
            }

            _logger.Info("Found {0} video file(s) with IDs for '{1}'", idToFile.Count, creator.Title);

            // Track which IDs were matched so the rest can be recorded as unmatched
            var matchedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var linked = 0;

            foreach (var channel in channels)
            {
                var contents = _contentService.GetByChannelId(channel.Id);

                foreach (var content in contents)
                {
                    if (content.ContentFileId > 0)
                    {
                        // Already linked — mark as matched so the file is not added to unmatched
                        matchedIds.Add(content.PlatformContentId);
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

                    matchedIds.Add(content.PlatformContentId);
                    linked++;
                    _logger.Debug("Linked '{0}' to content '{1}'", filePath, content.Title);
                }
            }

            // Files whose ID didn't match any known content record go to unmatched
            var unmatched = 0;
            foreach (var kvp in idToFile)
            {
                if (matchedIds.Contains(kvp.Key))
                {
                    continue;
                }

                if (existingUnmatchedPaths.Contains(kvp.Value))
                {
                    continue;
                }

                var fileInfo = new FileInfo(kvp.Value);
                _unmatchedFileService.Add(new UnmatchedFile
                {
                    CreatorId = creator.Id,
                    FilePath = kvp.Value,
                    FileName = Path.GetFileName(kvp.Value),
                    FileSize = fileInfo.Length,
                    DateFound = fileInfo.LastWriteTimeUtc,
                    Reason = UnmatchedFileReason.MetadataNotFound,
                });

                unmatched++;
                _logger.Debug("No content record for ID '{0}'; recorded as unmatched", kvp.Key);
            }

            _logger.Info(
                "Rescan complete for '{0}': {1} linked, {2} unmatched",
                creator.Title,
                linked,
                unmatched + noIdFiles.Count);
        }

        private void PruneStaleUnmatched(Creator creator, List<Channel> channels)
        {
            var existing = _unmatchedFileService.GetByCreatorId(creator.Id);
            if (!existing.Any())
            {
                return;
            }

            // Build the set of IDs already linked to a content file so we can
            // detect unmatched records that have since been resolved.
            var linkedIds = channels
                .SelectMany(ch => _contentService.GetByChannelId(ch.Id))
                .Where(c => c.ContentFileId > 0)
                .Select(c => c.PlatformContentId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var pruned = 0;

            foreach (var record in existing)
            {
                if (!File.Exists(record.FilePath))
                {
                    _unmatchedFileService.Delete(record.Id);
                    _logger.Debug("Pruned unmatched record for deleted file: {0}", record.FileName);
                    pruned++;
                    continue;
                }

                var idMatches = YouTubeIdRegex.Matches(record.FileName);
                var idMatch = idMatches.Count > 0 ? idMatches[idMatches.Count - 1] : null;
                if (idMatch != null && linkedIds.Contains(idMatch.Groups[1].Value))
                {
                    _unmatchedFileService.Delete(record.Id);
                    _logger.Debug("Pruned unmatched record for already-downloaded file: {0}", record.FileName);
                    pruned++;
                }
            }

            if (pruned > 0)
            {
                _logger.Info("Pruned {0} stale unmatched record(s) for creator '{1}'", pruned, creator.Title);
            }
        }

        private static void ScanVideoFiles(
            string dirPath,
            out Dictionary<string, string> idToFile,
            out List<string> noIdFiles)
        {
            idToFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            noIdFiles = new List<string>();

            foreach (var file in Directory.GetFiles(dirPath))
            {
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (!VideoExtensions.Contains(ext))
                {
                    continue;
                }

                // yt-dlp always appends the ID as the last bracketed token, so use
                // the last match to avoid false hits on bracketed words in the title
                // (e.g. "[Inscryption]" is 11 chars and would be matched first).
                var matches = YouTubeIdRegex.Matches(Path.GetFileName(file));
                if (matches.Count == 0)
                {
                    noIdFiles.Add(file);
                    continue;
                }

                var videoId = matches[matches.Count - 1].Groups[1].Value;
                if (!idToFile.ContainsKey(videoId))
                {
                    idToFile[videoId] = file;
                }
            }
        }
    }
}
