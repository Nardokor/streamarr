using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Core.Download;
using Streamarr.Core.Download.YtDlp;
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
    private readonly ILiveRecordingSupervisor _supervisor;
    private readonly IYtDlpClient _ytDlpClient;

    public QueueController(IManageCommandQueue commandQueueManager,
                           IContentService contentService,
                           IChannelService channelService,
                           ICreatorService creatorService,
                           ILiveRecordingSupervisor supervisor,
                           IYtDlpClient ytDlpClient)
    {
        _commandQueueManager = commandQueueManager;
        _contentService = contentService;
        _channelService = channelService;
        _creatorService = creatorService;
        _supervisor = supervisor;
        _ytDlpClient = ytDlpClient;
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
                    Message = command.Message ?? string.Empty,
                    QueuedAt = command.QueuedAt,
                    StartedAt = command.StartedAt,
                    State = ResolveState(command, content.Id)
                });
            }
            catch
            {
                // Content/channel/creator may have been deleted while command was in queue
            }
        }

        return resources;
    }

    [HttpGet("slots")]
    [Produces("application/json")]
    public QueueSlotsResource GetSlots()
    {
        var status = _ytDlpClient.GetSlotStatus();

        return new QueueSlotsResource
        {
            ConfiguredMax = status.ConfiguredMax,
            EffectiveMax = status.EffectiveMax,
            AvailableSlots = status.AvailableSlots,
            ActiveDownloadContentIds = status.ActiveDownloads.Select(d => d.ContentId).ToList(),
            LiveWaitingContentIds = _supervisor.GetSupervisedContentIds()
                .Where(id => !_ytDlpClient.IsDownloadActive(id))
                .ToList()
        };
    }

    [HttpDelete("{contentId:int}")]
    public IActionResult CancelDownload(int contentId)
    {
        // Routes through the supervisor so a live recording stops relaunching; for plain VOD
        // downloads (not supervised) it still kills the running yt-dlp process.
        _supervisor.Cancel(contentId);
        return Ok();
    }

    // Distinguishes what a "Started" command is actually doing, since CommandQueue flips status
    // to Started at dispatch — before any download slot is acquired. A command can be Started
    // yet still be blocked waiting for a free slot.
    private string ResolveState(CommandModel command, int contentId)
    {
        if (command.Status == CommandStatus.Queued)
        {
            return "queued";
        }

        if (_ytDlpClient.IsDownloadActive(contentId))
        {
            return "downloading";
        }

        if (_supervisor.IsSupervising(contentId))
        {
            return "liveWaiting";
        }

        return "waitingForSlot";
    }
}
