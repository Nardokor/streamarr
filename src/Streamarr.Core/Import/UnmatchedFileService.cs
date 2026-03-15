using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.MediaFiles;
using ContentEntity = Streamarr.Core.Content.Content;

namespace Streamarr.Core.Import
{
    public interface IUnmatchedFileService
    {
        List<UnmatchedFile> GetAll();
        List<UnmatchedFile> GetByCreatorId(int creatorId);
        UnmatchedFile Add(UnmatchedFile unmatchedFile);
        ContentEntity Assign(int unmatchedFileId, int channelId);
        void Delete(int id);
    }

    public class UnmatchedFileService : IUnmatchedFileService
    {
        private readonly IUnmatchedFileRepository _repo;
        private readonly IContentService _contentService;
        private readonly IContentFileService _contentFileService;
        private readonly IVideoThumbnailService _thumbnailService;
        private readonly Logger _logger;

        public UnmatchedFileService(
            IUnmatchedFileRepository repo,
            IContentService contentService,
            IContentFileService contentFileService,
            IVideoThumbnailService thumbnailService,
            Logger logger)
        {
            _repo = repo;
            _contentService = contentService;
            _contentFileService = contentFileService;
            _thumbnailService = thumbnailService;
            _logger = logger;
        }

        public List<UnmatchedFile> GetAll()
        {
            return _repo.All().ToList();
        }

        public List<UnmatchedFile> GetByCreatorId(int creatorId)
        {
            return _repo.GetByCreatorId(creatorId);
        }

        public UnmatchedFile Add(UnmatchedFile unmatchedFile)
        {
            var existing = _repo.FindByFilePath(unmatchedFile.FilePath);
            if (existing != null)
            {
                return existing;
            }

            _logger.Debug("Recording unmatched file '{0}' (reason: {1})", unmatchedFile.FileName, unmatchedFile.Reason);
            return _repo.Insert(unmatchedFile);
        }

        public ContentEntity Assign(int unmatchedFileId, int channelId)
        {
            var file = _repo.Get(unmatchedFileId);

            var fileInfo = new FileInfo(file.FilePath);

            // Use LastWriteTimeUtc (mtime) — on Linux, CreationTimeUtc maps to ctime
            // which resets on every copy/move. mtime is preserved by cp, rsync, etc.
            var airDate = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.UtcNow;
            var title = Path.GetFileNameWithoutExtension(file.FileName);
            var platformId = $"local-{Guid.NewGuid():N}";

            var content = _contentService.AddContent(new ContentEntity
            {
                ChannelId = channelId,
                PlatformContentId = platformId,
                ContentType = ContentType.Video,
                Title = title,
                Description = string.Empty,
                ThumbnailUrl = string.Empty,
                AirDateUtc = airDate,
                DateAdded = DateTime.UtcNow,
                Monitored = true,
                Status = ContentStatus.Downloaded,
            });

            var contentFile = _contentFileService.AddContentFile(new ContentFile
            {
                ContentId = content.Id,
                RelativePath = file.FileName,
                Size = file.FileSize,
                DateAdded = DateTime.UtcNow,
                OriginalFilePath = file.FilePath,
            });

            content.ContentFileId = contentFile.Id;
            _contentService.UpdateContent(content);

            var thumbnailUrl = _thumbnailService.GenerateThumbnail(content.Id, file.FilePath);
            if (!string.IsNullOrEmpty(thumbnailUrl))
            {
                content.ThumbnailUrl = thumbnailUrl;
                _contentService.UpdateContent(content);
            }

            _repo.Delete(unmatchedFileId);

            _logger.Info("Manually assigned '{0}' to channel {1} as content {2}", file.FileName, channelId, content.Id);
            return content;
        }

        public void Delete(int id)
        {
            _repo.Delete(id);
        }
    }
}
