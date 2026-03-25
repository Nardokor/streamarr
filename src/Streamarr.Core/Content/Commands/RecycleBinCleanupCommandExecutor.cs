using System;
using System.IO;
using NLog;
using Streamarr.Common.Disk;
using Streamarr.Core.Configuration;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.RootFolders;

namespace Streamarr.Core.Content.Commands
{
    public class RecycleBinCleanupCommandExecutor : IExecute<RecycleBinCleanupCommand>
    {
        private readonly IRootFolderService _rootFolderService;
        private readonly IDiskProvider _diskProvider;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public RecycleBinCleanupCommandExecutor(
            IRootFolderService rootFolderService,
            IDiskProvider diskProvider,
            IConfigService configService,
            Logger logger)
        {
            _rootFolderService = rootFolderService;
            _diskProvider = diskProvider;
            _configService = configService;
            _logger = logger;
        }

        public void Execute(RecycleBinCleanupCommand message)
        {
            var cleanupDays = _configService.RecycleBinCleanupDays;
            if (cleanupDays <= 0)
            {
                _logger.Debug("Recycle bin cleanup is disabled (RecycleBinCleanupDays = {0})", cleanupDays);
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-cleanupDays);
            var rootFolders = _rootFolderService.All();
            var totalDeleted = 0;

            foreach (var rootFolder in rootFolders)
            {
                var recycleBinPath = Path.Combine(rootFolder.Path, ".recycle");

                if (!_diskProvider.FolderExists(recycleBinPath))
                {
                    continue;
                }

                foreach (var file in _diskProvider.GetFiles(recycleBinPath, false))
                {
                    if (file.EndsWith(".recycledat", StringComparison.OrdinalIgnoreCase))
                    {
                        // Clean up orphaned sidecars (video file was manually removed)
                        var videoPath = file.Substring(0, file.Length - ".recycledat".Length);
                        if (!_diskProvider.FileExists(videoPath))
                        {
                            try
                            {
                                _diskProvider.DeleteFile(file);
                            }
                            catch
                            {
                                // best-effort
                            }
                        }

                        continue;
                    }

                    try
                    {
                        var sidecarPath = file + ".recycledat";
                        DateTime recycledAt;
                        if (_diskProvider.FileExists(sidecarPath) &&
                            DateTime.TryParse(_diskProvider.ReadAllText(sidecarPath), out var parsed))
                        {
                            recycledAt = parsed.ToUniversalTime();
                        }
                        else
                        {
                            recycledAt = _diskProvider.FileGetLastWrite(file);
                        }

                        if (recycledAt > cutoff)
                        {
                            continue;
                        }

                        _diskProvider.DeleteFile(file);
                        var sidecar = file + ".recycledat";
                        if (_diskProvider.FileExists(sidecar))
                        {
                            _diskProvider.DeleteFile(sidecar);
                        }

                        totalDeleted++;
                        _logger.Debug("Permanently deleted recycled file '{0}' (recycled: {1:d})", file, recycledAt);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to delete recycled file '{0}'", file);
                    }
                }
            }

            if (totalDeleted > 0)
            {
                _logger.Info("Recycle bin cleanup complete: {0} file(s) permanently deleted", totalDeleted);
            }
        }
    }
}
