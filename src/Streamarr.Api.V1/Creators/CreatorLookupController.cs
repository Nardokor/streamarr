using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.MetadataSource;
using Streamarr.Http;

namespace Streamarr.Api.V1.Creators;

[V1ApiController("creator/lookup")]
public class CreatorLookupController : Controller
{
    private readonly MetadataSourceFactory _metadataSourceFactory;
    private readonly IChannelService _channelService;

    public CreatorLookupController(MetadataSourceFactory metadataSourceFactory,
                                   IChannelService channelService)
    {
        _metadataSourceFactory = metadataSourceFactory;
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

        // Try each enabled source and return the first successful result
        foreach (var definition in _metadataSourceFactory.All().Where(d => d.Enable))
        {
            try
            {
                var source = _metadataSourceFactory.GetInstance(definition);
                var result = source.SearchCreator(term);

                var existingChannel = result.Channels
                    .Select(ch => _channelService.FindByPlatformId(ch.Platform, ch.PlatformId))
                    .FirstOrDefault(ch => ch != null);

                if (existingChannel != null)
                {
                    result.ExistingCreatorId = existingChannel.CreatorId;
                }

                return Ok(result);
            }
            catch
            {
                // Try next source
            }
        }

        return NotFound("No results found");
    }
}
