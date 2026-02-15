using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Organizer;
using Streamarr.Core.Tv;
using Streamarr.Http;

namespace Streamarr.Api.V1.Series;

[V1ApiController("series")]
public class SeriesFolderController : Controller
{
    private readonly ISeriesService _seriesService;
    private readonly IBuildFileNames _fileNameBuilder;

    public SeriesFolderController(ISeriesService seriesService, IBuildFileNames fileNameBuilder)
    {
        _seriesService = seriesService;
        _fileNameBuilder = fileNameBuilder;
    }

    [HttpGet("{id}/folder")]
    [Produces("application/json")]
    public object GetFolder([FromRoute] int id)
    {
        var series = _seriesService.GetSeries(id);
        var folder = _fileNameBuilder.GetSeriesFolder(series);

        return new
        {
            folder
        };
    }
}
