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
    public ActionResult<CreatorMetadataResult> Lookup([FromQuery] string term, [FromQuery] string? platform = null)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return BadRequest("term is required");
        }

        IEnumerable<IMetadataSource> sources;

        if (platform != null)
        {
            if (!Enum.TryParse<PlatformType>(platform, ignoreCase: true, out var platformType) || platformType == PlatformType.Unknown)
            {
                return BadRequest($"Unknown platform: {platform}");
            }

            var platformSource = _metadataSourceFactory.GetByPlatform(platformType);
            if (platformSource == null)
            {
                return NotFound($"No enabled source for platform: {platform}");
            }

            sources = new[] { platformSource };
        }
        else
        {
            sources = _metadataSourceFactory.All()
                .Where(d => d.Enable)
                .Select(d => _metadataSourceFactory.GetInstance(d));
        }

        foreach (var source in sources)
        {
            try
            {
                var result = source.SearchCreator(term);

                var existingChannel = result.Channels
                    .Select(ch => _channelService.FindByPlatformId(ch.Platform, ch.PlatformId))
                    .FirstOrDefault(ch => ch != null);

                if (existingChannel != null)
                {
                    result.ExistingCreatorId = existingChannel.CreatorId;
                }

                if (result.Channels?.Count > 0)
                {
                    return Ok(result);
                }
            }
            catch
            {
                // Try next source
            }
        }

        return NotFound("No results found");
    }
}
