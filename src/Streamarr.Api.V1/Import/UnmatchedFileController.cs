using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Import;
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

    public UnmatchedFileController(IUnmatchedFileService unmatchedFileService)
    {
        _unmatchedFileService = unmatchedFileService;
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
        return Ok();
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
            return RemuxAndStream(file.FilePath, id);
        }

        return PhysicalFile(file.FilePath, mimeType, enableRangeProcessing: true);
    }

    private IActionResult RemuxAndStream(string sourcePath, int id)
    {
        var tempDir = IO.Path.Combine(IO.Path.GetTempPath(), "streamarr-remux");
        IO.Directory.CreateDirectory(tempDir);

        var tempPath = IO.Path.Combine(tempDir, $"{id}_{Guid.NewGuid():N}.mp4");

        // sourcePath comes from the database; the temp filename uses only a server-generated GUID
#pragma warning disable CA3006
        var psi = new ProcessStartInfo("ffmpeg")
        {
            Arguments = $"-i \"{sourcePath}\" -c copy -movflags +faststart -f mp4 -y \"{tempPath}\"",
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
#pragma warning restore CA3006

        using var process = Process.Start(psi);
        process?.WaitForExit();

        // Path comes from the database / server-generated temp path
#pragma warning disable CA3003
        if (process?.ExitCode != 0 || !IO.File.Exists(tempPath))
        {
            return StatusCode(500, "Remux failed");
        }
#pragma warning restore CA3003

        HttpContext.Response.OnCompleted(() =>
        {
            try
            {
                IO.File.Delete(tempPath);
            }
            catch
            {
                // best-effort cleanup
            }

            return Task.CompletedTask;
        });

        return PhysicalFile(tempPath, "video/mp4", enableRangeProcessing: true);
    }
}

public class AssignUnmatchedRequest
{
    public int ChannelId { get; set; }
}
