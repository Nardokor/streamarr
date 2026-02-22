using Microsoft.AspNetCore.Mvc;
using Streamarr.Api.V1.Contents;
using Streamarr.Core.Content;
using Streamarr.Http;

namespace Streamarr.Api.V1.Wanted;

[V1ApiController("wanted")]
public class WantedController : Controller
{
    private readonly IContentService _contentService;

    public WantedController(IContentService contentService)
    {
        _contentService = contentService;
    }

    [HttpGet("missing")]
    [Produces("application/json")]
    public List<ContentResource> GetMissing()
    {
        return _contentService.GetAllMissing().ToResource();
    }
}
