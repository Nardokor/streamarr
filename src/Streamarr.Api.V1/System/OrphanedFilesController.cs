using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Download;
using Streamarr.Http;

namespace Streamarr.Api.V1.System;

[V1ApiController("system/orphanedfiles")]
public class OrphanedFilesController : Controller
{
    private readonly IOrphanedFileScanner _scanner;

    public OrphanedFilesController(IOrphanedFileScanner scanner)
    {
        _scanner = scanner;
    }

    [HttpGet]
    [Produces("application/json")]
    public List<OrphanedFile> Scan()
    {
        return _scanner.Scan();
    }

    [HttpDelete]
    public IActionResult Delete([FromQuery] string path)
    {
        _scanner.Delete(path);
        return NoContent();
    }
}
