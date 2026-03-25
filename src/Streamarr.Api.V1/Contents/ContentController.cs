using Microsoft.AspNetCore.Mvc;
using Streamarr.Common.Disk;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.ContentFiles;
using Streamarr.Core.Creators;
using Streamarr.Core.RootFolders;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;
using IO = System.IO;

namespace Streamarr.Api.V1.Contents;

[V1ApiController]
public class ContentController : RestControllerWithSignalR<ContentResource, Content>
{
    private readonly IContentService _contentService;
    private readonly IChannelService _channelService;
    private readonly IContentFileService _contentFileService;
    private readonly ICreatorService _creatorService;
    private readonly IRootFolderService _rootFolderService;
    private readonly IDiskProvider _diskProvider;

    public ContentController(IContentService contentService,
                             IChannelService channelService,
                             IContentFileService contentFileService,
                             ICreatorService creatorService,
                             IRootFolderService rootFolderService,
                             IDiskProvider diskProvider,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(signalRBroadcaster)
    {
        _contentService = contentService;
        _channelService = channelService;
        _contentFileService = contentFileService;
        _creatorService = creatorService;
        _rootFolderService = rootFolderService;
        _diskProvider = diskProvider;
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

    [HttpDelete("{id:int}/file")]
    public ActionResult DeleteFile(int id)
    {
        var content = _contentService.GetContent(id);
        if (content == null)
        {
            return NotFound();
        }

        if (content.ContentFileId == 0)
        {
            return BadRequest("Content has no associated file.");
        }

        var contentFile = _contentFileService.GetContentFile(content.ContentFileId);
        var channel = _channelService.GetChannel(content.ChannelId);
        var creator = _creatorService.GetCreator(channel.CreatorId);
        var fullPath = IO.Path.Combine(creator.Path, contentFile.RelativePath);
        var rootFolderPath = _rootFolderService.GetBestRootFolderPath(creator.Path);
        var recycleBinPath = IO.Path.Combine(rootFolderPath, ".recycle");

        if (_diskProvider.FileExists(fullPath))
        {
            _diskProvider.MoveToRecycleBin(fullPath, recycleBinPath);
        }

        _contentFileService.DeleteContentFile(contentFile.Id);
        content.ContentFileId = 0;
        content.Status = ContentStatus.Available;
        _contentService.UpdateContent(content);

        return NoContent();
    }
}
