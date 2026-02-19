using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Http;

namespace Streamarr.Api.V1.Creators;

[V1ApiController("creator/lookup")]
public class CreatorLookupController : Controller
{
    private readonly YouTubeMetadataService _metadataService;

    public CreatorLookupController(YouTubeMetadataService metadataService)
    {
        _metadataService = metadataService;
    }

    [HttpGet]
    [Produces("application/json")]
    public ActionResult<CreatorMetadataResult> Lookup([FromQuery] string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest("term is required");
        }

        var result = _metadataService.SearchCreator(term);
        return Ok(result);
    }
}
