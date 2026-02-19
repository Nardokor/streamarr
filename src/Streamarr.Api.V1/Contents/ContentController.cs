using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Content;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Contents;

[V1ApiController]
public class ContentController : RestControllerWithSignalR<ContentResource, Content>
{
    private readonly IContentService _contentService;

    public ContentController(IContentService contentService,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(signalRBroadcaster)
    {
        _contentService = contentService;
    }

    protected override ContentResource GetResourceById(int id)
    {
        return _contentService.GetContent(id).ToResource();
    }

    [HttpGet("channel/{channelId:int}")]
    [Produces("application/json")]
    public List<ContentResource> GetByChannel(int channelId)
    {
        return _contentService.GetByChannelId(channelId).ToResource();
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<ContentResource> Update([FromBody] ContentResource resource)
    {
        _contentService.UpdateContent(resource.ToModel());
        return Accepted(resource.Id);
    }

    [RestDeleteById]
    public ActionResult Delete(int id)
    {
        _contentService.DeleteContent(id);
        return NoContent();
    }
}
