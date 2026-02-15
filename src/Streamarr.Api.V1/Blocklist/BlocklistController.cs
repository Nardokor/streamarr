using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Blocklisting;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.Datastore;
using Streamarr.Core.Indexers;
using Streamarr.Http;
using Streamarr.Http.Extensions;
using Streamarr.Http.REST.Attributes;

namespace Streamarr.Api.V1.Blocklist;

[V1ApiController]
public class BlocklistController : Controller
{
    private readonly IBlocklistService _blocklistService;
    private readonly ICustomFormatCalculationService _formatCalculator;

    public BlocklistController(IBlocklistService blocklistService,
                               ICustomFormatCalculationService formatCalculator)
    {
        _blocklistService = blocklistService;
        _formatCalculator = formatCalculator;
    }

    [HttpGet]
    [Produces("application/json")]
    public PagingResource<BlocklistResource> GetBlocklist([FromQuery] PagingRequestResource paging, [FromQuery] int[]? seriesIds = null, [FromQuery] DownloadProtocol[]? protocols = null)
    {
        var pagingResource = new PagingResource<BlocklistResource>(paging);
        var pagingSpec = pagingResource.MapToPagingSpec<BlocklistResource, Streamarr.Core.Blocklisting.Blocklist>(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "date",
                "indexer",
                "series.sortTitle",
                "sourceTitle"
            },
            "date",
            SortDirection.Descending);

        if (seriesIds?.Any() == true)
        {
            pagingSpec.FilterExpressions.Add(b => seriesIds.Contains(b.SeriesId));
        }

        if (protocols?.Any() == true)
        {
            pagingSpec.FilterExpressions.Add(b => protocols.Contains(b.Protocol));
        }

        return pagingSpec.ApplyToPage(b => _blocklistService.Paged(pagingSpec), b => BlocklistResourceMapper.MapToResource(b, _formatCalculator));
    }

    [RestDeleteById]
    public ActionResult DeleteBlocklist(int id)
    {
        _blocklistService.Delete(id);

        return NoContent();
    }

    [HttpDelete("bulk")]
    [Produces("application/json")]
    public ActionResult Remove([FromBody] BlocklistBulkResource resource)
    {
        _blocklistService.Delete(resource.Ids);

        return NoContent();
    }
}
