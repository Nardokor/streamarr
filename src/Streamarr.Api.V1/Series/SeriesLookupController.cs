using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.MediaCover;
using Streamarr.Core.MetadataSource;
using Streamarr.Core.Organizer;
using Streamarr.Core.SeriesStats;
using Streamarr.Http;

namespace Streamarr.Api.V1.Series;

[V1ApiController("series/lookup")]
public class SeriesLookupController : Controller
{
    private readonly ISearchForNewSeries _searchProxy;
    private readonly IBuildFileNames _fileNameBuilder;
    private readonly IMapCoversToLocal _coverMapper;

    public SeriesLookupController(ISearchForNewSeries searchProxy, IBuildFileNames fileNameBuilder, IMapCoversToLocal coverMapper)
    {
        _searchProxy = searchProxy;
        _fileNameBuilder = fileNameBuilder;
        _coverMapper = coverMapper;
    }

    [HttpGet]
    public IEnumerable<SeriesResource> Search([FromQuery] string term)
    {
        var tvDbResults = _searchProxy.SearchForNewSeries(term);
        return MapToResource(tvDbResults);
    }

    private IEnumerable<SeriesResource> MapToResource(IEnumerable<Streamarr.Core.Tv.Series> series)
    {
        foreach (var currentSeries in series)
        {
            var resource = currentSeries.ToResource();

            _coverMapper.ConvertToLocalUrls(resource.Id, resource.Images);

            var poster = currentSeries.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);

            if (poster != null)
            {
                resource.RemotePoster = poster.RemoteUrl;
            }

            resource.Folder = _fileNameBuilder.GetSeriesFolder(currentSeries);
            resource.Statistics = new SeriesStatistics().ToResource(resource.Seasons);

            yield return resource;
        }
    }
}
