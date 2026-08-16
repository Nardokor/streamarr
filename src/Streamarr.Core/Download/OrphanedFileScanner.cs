using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Core.Creators;
using Streamarr.Core.Download.YtDlp;

namespace Streamarr.Core.Download
{
    public class OrphanedFile
    {
        public string Path { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public string CreatorTitle { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastWriteUtc { get; set; }

        // True once the file hasn't been touched in a while — a fragment still being written by
        // an active download has its mtime updated continuously, so a stale mtime is a reasonable
        // signal that the download that created it is no longer running.
        public bool Stale { get; set; }
    }

    public interface IOrphanedFileScanner
    {
        List<OrphanedFile> Scan();
        void Delete(string path);
    }

    public class OrphanedFileScanner : IOrphanedFileScanner
    {
        private static readonly TimeSpan StaleThreshold = TimeSpan.FromHours(2);

        private readonly ICreatorService _creatorService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public OrphanedFileScanner(ICreatorService creatorService, IDiskProvider diskProvider, Logger logger)
        {
            _creatorService = creatorService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<OrphanedFile> Scan()
        {
            var results = new List<OrphanedFile>();
            var now = DateTime.UtcNow;

            foreach (var creator in _creatorService.GetAllCreators())
            {
                if (string.IsNullOrWhiteSpace(creator.Path) || !_diskProvider.FolderExists(creator.Path))
                {
                    continue;
                }

                IEnumerable<string> files;
                try
                {
                    files = _diskProvider.GetFiles(creator.Path, recursive: true);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to scan '{0}' for orphaned files", creator.Path);
                    continue;
                }

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    if (!YtDlpFileClassifier.IsIntermediate(fileName))
                    {
                        continue;
                    }

                    DateTime lastWrite;
                    long size;
                    try
                    {
                        lastWrite = _diskProvider.FileGetLastWrite(file).ToUniversalTime();
                        size = _diskProvider.GetFileSize(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to stat orphaned file candidate '{0}'", file);
                        continue;
                    }

                    results.Add(new OrphanedFile
                    {
                        Path = file,
                        FileName = fileName,
                        CreatorId = creator.Id,
                        CreatorTitle = creator.Title,
                        Size = size,
                        LastWriteUtc = lastWrite,
                        Stale = now - lastWrite > StaleThreshold
                    });
                }
            }

            return results.OrderByDescending(f => f.LastWriteUtc).ToList();
        }

        public void Delete(string path)
        {
            // Only ever called with a path returned by Scan(), which enumerates files strictly
            // under a known creator's folder — never accept a caller-supplied arbitrary path here.
            var isUnderKnownCreator = _creatorService.GetAllCreators()
                .Any(c => !string.IsNullOrWhiteSpace(c.Path) &&
                          path.StartsWith(c.Path, StringComparison.Ordinal));

            if (!isUnderKnownCreator)
            {
                throw new InvalidOperationException($"Refusing to delete path outside known creator folders: {path}");
            }

            if (_diskProvider.FileExists(path))
            {
                _diskProvider.DeleteFile(path);
                _logger.Info("Deleted orphaned file '{0}'", path);
            }
        }
    }
}
