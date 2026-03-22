using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Common.Disk;
using Streamarr.Core.Creators;
using Streamarr.Core.Import;
using Streamarr.Core.RootFolders;
using Streamarr.Http;
using IO = System.IO;

namespace Streamarr.Api.V1.Import;

[V1ApiController("unmatchedfile")]
public class UnmatchedFileController : Controller
{
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".mp4",  "video/mp4" },
        { ".m4v",  "video/mp4" },
        { ".webm", "video/webm" },
        { ".mov",  "video/quicktime" },
        { ".avi",  "video/x-msvideo" },
        { ".ts",   "video/mp2t" },
        { ".flv",  "video/x-flv" },
        { ".mkv",  "video/mp4" },  // remuxed to MP4 below
    };

    private readonly IUnmatchedFileService _unmatchedFileService;
    private readonly ICreatorService _creatorService;
    private readonly IRootFolderService _rootFolderService;
    private readonly IDiskProvider _diskProvider;

    public UnmatchedFileController(
        IUnmatchedFileService unmatchedFileService,
        ICreatorService creatorService,
        IRootFolderService rootFolderService,
        IDiskProvider diskProvider)
    {
        _unmatchedFileService = unmatchedFileService;
        _creatorService = creatorService;
        _rootFolderService = rootFolderService;
        _diskProvider = diskProvider;
    }

    [HttpGet]
    [Produces("application/json")]
    public List<UnmatchedFile> GetAll()
    {
        return _unmatchedFileService.GetAll();
    }

    [HttpGet("creator/{creatorId:int}")]
    [Produces("application/json")]
    public List<UnmatchedFile> GetByCreator(int creatorId)
    {
        return _unmatchedFileService.GetByCreatorId(creatorId);
    }

    [HttpPost("{id:int}/assign")]
    [Produces("application/json")]
    public IActionResult Assign(int id, [FromBody] AssignUnmatchedRequest request)
    {
        var content = _unmatchedFileService.Assign(id, request.ChannelId);
        return Ok(content);
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _unmatchedFileService.Delete(id);
        return NoContent();
    }

    [HttpDelete("{id:int}/file")]
    public IActionResult DeleteFile(int id)
    {
        var file = _unmatchedFileService.GetById(id);
        if (file == null)
        {
            return NotFound();
        }

        var creator = _creatorService.GetCreator(file.CreatorId);
        var rootFolderPath = _rootFolderService.GetBestRootFolderPath(creator.Path);
        var recycleBinPath = IO.Path.Combine(rootFolderPath, ".recycle");

        // FilePath comes from the database, not directly from the user-supplied id
#pragma warning disable CA3003
        if (IO.File.Exists(file.FilePath))
        {
            _diskProvider.MoveToRecycleBin(file.FilePath, recycleBinPath);
        }
#pragma warning restore CA3003

        _unmatchedFileService.Delete(id);
        return NoContent();
    }

    [HttpGet("{id:int}/stream")]
    public IActionResult Stream(int id)
    {
        var file = _unmatchedFileService.GetById(id);

        // Path comes from the database (not directly from the user-supplied id)
#pragma warning disable CA3003
        if (file == null || !IO.File.Exists(file.FilePath))
        {
            return NotFound();
        }
#pragma warning restore CA3003

        var ext = IO.Path.GetExtension(file.FilePath).ToLowerInvariant();
        if (!MimeTypes.TryGetValue(ext, out var mimeType))
        {
            return StatusCode(415);
        }

        if (ext == ".mkv")
        {
            return RemuxAndStream(file.FilePath);
        }

        return PhysicalFile(file.FilePath, mimeType, enableRangeProcessing: true);
    }

    private IActionResult RemuxAndStream(string sourcePath)
    {
        // sourcePath comes from the database, not directly from user input
#pragma warning disable CA3006
        var psi = new ProcessStartInfo("ffmpeg")
        {
            // frag_keyframe+empty_moov produces a fragmented MP4 that can be piped
            // without knowing the file size — playback starts immediately
            Arguments = $"-i \"{sourcePath}\" -c copy -f mp4 -movflags frag_keyframe+empty_moov pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
#pragma warning restore CA3006

        var process = Process.Start(psi);
        if (process == null)
        {
            return StatusCode(500, "Failed to start ffmpeg");
        }

        HttpContext.Response.OnCompleted(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                process.Dispose();
            }
            catch
            {
                // best-effort cleanup
            }

            return Task.CompletedTask;
        });

        return File(process.StandardOutput.BaseStream, "video/mp4");
    }
}

public class AssignUnmatchedRequest
{
    public int ChannelId { get; set; }
}
