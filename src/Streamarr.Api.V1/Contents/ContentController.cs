using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Contents;

[V1ApiController]
public class ContentController : RestControllerWithSignalR<ContentResource, Content>
{
    private readonly IContentService _contentService;
    private readonly IChannelService _channelService;
    private readonly IContentFileService _contentFileService;

    public ContentController(IContentService contentService,
                             IChannelService channelService,
                             IContentFileService contentFileService,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(signalRBroadcaster)
    {
        _contentService = contentService;
        _channelService = channelService;
        _contentFileService = contentFileService;
    }

    protected override ContentResource GetResourceById(int id)
    {
        var resource = _contentService.GetContent(id).ToResource();

        if (resource.ContentFileId > 0)
        {
            var file = _contentFileService.GetContentFile(resource.ContentFileId);
            resource.FileRelativePath = file?.RelativePath;
            resource.FileSize = file?.Size;
        }

        return resource;
    }

    [HttpGet("channel/{channelId:int}")]
    [Produces("application/json")]
    public List<ContentResource> GetByChannel(int channelId)
    {
        return _contentService.GetByChannelId(channelId).ToResource();
    }

    [HttpGet("creator/{creatorId:int}")]
    [Produces("application/json")]
    public List<ContentResource> GetByCreator(int creatorId)
    {
        var channels = _channelService.GetByCreatorId(creatorId);
        return channels.SelectMany(ch => _contentService.GetByChannelId(ch.Id)).ToResource();
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
