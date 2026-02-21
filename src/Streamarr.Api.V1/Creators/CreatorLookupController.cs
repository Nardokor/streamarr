using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Http;

namespace Streamarr.Api.V1.Creators;

[V1ApiController("creator/lookup")]
public class CreatorLookupController : Controller
{
    private readonly YouTubeMetadataService _metadataService;
    private readonly IChannelService _channelService;

    public CreatorLookupController(YouTubeMetadataService metadataService,
                                   IChannelService channelService)
    {
        _metadataService = metadataService;
        _channelService = channelService;
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

        // Check whether any returned channel already exists in the library.
        var existingChannel = result.Channels
            .Select(ch => _channelService.FindByPlatformId(ch.Platform, ch.PlatformId))
            .FirstOrDefault(ch => ch != null);

        if (existingChannel != null)
        {
            result.ExistingCreatorId = existingChannel.CreatorId;
        }

        return Ok(result);
    }
}
