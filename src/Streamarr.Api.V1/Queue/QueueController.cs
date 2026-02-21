using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Core.Download;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Http;

namespace Streamarr.Api.V1.Queue;

[V1ApiController]
public class QueueController : Controller
{
    private readonly IManageCommandQueue _commandQueueManager;
    private readonly IContentService _contentService;
    private readonly IChannelService _channelService;
    private readonly ICreatorService _creatorService;

    public QueueController(IManageCommandQueue commandQueueManager,
                           IContentService contentService,
                           IChannelService channelService,
                           ICreatorService creatorService)
    {
        _commandQueueManager = commandQueueManager;
        _contentService = contentService;
        _channelService = channelService;
        _creatorService = creatorService;
    }

    [HttpGet]
    [Produces("application/json")]
    public List<QueueResource> GetQueue()
    {
        var commands = _commandQueueManager.All()
            .Where(c => c.Name == "DownloadContent" &&
                        (c.Status == CommandStatus.Queued || c.Status == CommandStatus.Started))
            .ToList();

        var resources = new List<QueueResource>();

        foreach (var command in commands)
        {
            if (command.Body is not DownloadContentCommand downloadCommand)
            {
                continue;
            }

            try
            {
                var content = _contentService.GetContent(downloadCommand.ContentId);
                var channel = _channelService.GetChannel(content.ChannelId);
                var creator = _creatorService.GetCreator(channel.CreatorId);

                resources.Add(new QueueResource
                {
                    CommandId = command.Id,
                    ContentId = content.Id,
                    ContentTitle = content.Title,
                    ThumbnailUrl = content.ThumbnailUrl,
                    CreatorName = creator.Title,
                    ChannelName = channel.Title,
                    Status = command.Status.ToString().ToLowerInvariant(),
                    Message = command.Message ?? string.Empty
                });
            }
            catch
            {
                // Content/channel/creator may have been deleted while command was in queue
            }
        }

        return resources;
    }
}
