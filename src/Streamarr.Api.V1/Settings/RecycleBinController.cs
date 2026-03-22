using Microsoft.AspNetCore.Mvc;
using Streamarr.Common.Disk;
using Streamarr.Core.Configuration;
using Streamarr.Core.RootFolders;
using Streamarr.Http;
using IO = System.IO;

namespace Streamarr.Api.V1.Settings;

[V1ApiController("recyclebin")]
public class RecycleBinController : Controller
{
    private readonly IRootFolderService _rootFolderService;
    private readonly IDiskProvider _diskProvider;
    private readonly IConfigService _configService;

    public RecycleBinController(
        IRootFolderService rootFolderService,
        IDiskProvider diskProvider,
        IConfigService configService)
    {
        _rootFolderService = rootFolderService;
        _diskProvider = diskProvider;
        _configService = configService;
    }

    [HttpGet]
    [Produces("application/json")]
    public List<RecycleBinItemResource> GetAll()
    {
        var cleanupDays = _configService.RecycleBinCleanupDays;
        var results = new List<RecycleBinItemResource>();

        foreach (var rootFolder in _rootFolderService.All())
        {
            var recycleBinPath = IO.Path.Combine(rootFolder.Path, ".recycle");
            if (!_diskProvider.FolderExists(recycleBinPath))
            {
                continue;
            }

            foreach (var file in _diskProvider.GetFiles(recycleBinPath, false))
            {
                if (file.EndsWith(".recycledat", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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

                DateTime? expiresAt = cleanupDays > 0
                    ? recycledAt.AddDays(cleanupDays)
                    : null;

                results.Add(new RecycleBinItemResource
                {
                    FileName = IO.Path.GetFileName(file),
                    FileSize = _diskProvider.GetFileSize(file),
                    RootFolderPath = rootFolder.Path,
                    RecycledAt = recycledAt,
                    ExpiresAt = expiresAt,
                });
            }
        }

        results.Sort((a, b) => a.RecycledAt.CompareTo(b.RecycledAt));
        return results;
    }
}

public class RecycleBinItemResource
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string RootFolderPath { get; set; } = string.Empty;
    public DateTime RecycledAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
