using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Datastore;
using Streamarr.Core.History;
using Streamarr.Http;
using Streamarr.Http.REST;

namespace Streamarr.Api.V1.History;

[V1ApiController]
public class HistoryController : RestController<HistoryResource>
{
    private readonly IDownloadHistoryService _historyService;

    public HistoryController(IDownloadHistoryService historyService)
    {
        _historyService = historyService;
    }

    protected override HistoryResource GetResourceById(int id)
    {
        var history = _historyService.GetAll().FirstOrDefault(h => h.Id == id);
        if (history == null)
        {
            throw new ModelNotFoundException(typeof(DownloadHistory), id);
        }

        return history.ToResource();
    }

    [HttpGet]
    [Produces("application/json")]
    public List<HistoryResource> GetHistory()
    {
        return _historyService.GetAll()
            .OrderByDescending(h => h.Date)
            .Select(h => h.ToResource())
            .ToList();
    }

    [HttpGet("creator/{creatorId:int}")]
    [Produces("application/json")]
    public List<HistoryResource> GetByCreator(int creatorId)
    {
        return _historyService.GetByCreatorId(creatorId)
            .OrderByDescending(h => h.Date)
            .Select(h => h.ToResource())
            .ToList();
    }
}
