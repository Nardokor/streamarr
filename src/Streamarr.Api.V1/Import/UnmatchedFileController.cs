using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Import;
using Streamarr.Http;

namespace Streamarr.Api.V1.Import;

[V1ApiController("unmatchedfile")]
public class UnmatchedFileController : Controller
{
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

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _unmatchedFileService.Delete(id);
        return Ok();
    }
}
