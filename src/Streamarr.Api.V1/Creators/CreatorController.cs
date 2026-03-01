using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.Creators;
using Streamarr.Core.Creators.Events;
using Streamarr.Core.Datastore.Events;
using Streamarr.Core.Messaging.Events;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Creators;

[V1ApiController]
public class CreatorController : RestControllerWithSignalR<CreatorResource, Creator>,
                                 IHandle<CreatorAddedEvent>,
                                 IHandle<CreatorUpdatedEvent>,
                                 IHandle<CreatorDeletedEvent>
{
    private readonly ICreatorService _creatorService;
    private readonly IChannelService _channelService;
    private readonly ICreatorAvatarService _creatorAvatarService;

    public CreatorController(ICreatorService creatorService,
                             IChannelService channelService,
                             ICreatorAvatarService creatorAvatarService,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(signalRBroadcaster)
    {
        _creatorService = creatorService;
        _channelService = channelService;
        _creatorAvatarService = creatorAvatarService;

        SharedValidator.RuleFor(c => c.Title).NotEmpty();
        SharedValidator.RuleFor(c => c.Path).NotEmpty();
    }

    protected override CreatorResource GetResourceById(int id)
    {
        return _creatorService.GetCreator(id).ToResource();
    }

    [HttpGet]
    [Produces("application/json")]
    public List<CreatorResource> GetAll()
    {
        return _creatorService.GetAllCreators().ToResource();
    }

    [HttpGet("slug/{slug}")]
    [Produces("application/json")]
    public ActionResult<CreatorResource> GetBySlug(string slug)
    {
        var creator = _creatorService.GetAllCreators()
            .FirstOrDefault(c => CreatorResourceMapper.Slugify(c.Title) == slug);
        if (creator == null)
        {
            return NotFound();
        }

        return Ok(creator.ToResource());
    }

    [RestPostById]
    [Consumes("application/json")]
    public ActionResult<CreatorResource> Create([FromBody] CreatorResource resource)
    {
        var creator = _creatorService.AddCreator(resource.ToModel());

        foreach (var ch in resource.Channels)
        {
            _channelService.AddChannel(new Channel
            {
                CreatorId = creator.Id,
                Platform = ch.Platform,
                PlatformId = ch.PlatformId,
                PlatformUrl = ch.PlatformUrl,
                Title = ch.Title,
                Description = ch.Description,
                ThumbnailUrl = ch.ThumbnailUrl,
                Monitored = true,
            });
        }

        if (!string.IsNullOrEmpty(resource.ThumbnailUrl))
        {
            _creatorAvatarService.DownloadAvatar(creator);
        }

        return Created(creator.Id);
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<CreatorResource> Update([FromBody] CreatorResource resource)
    {
        _creatorService.UpdateCreator(resource.ToModel());
        return Accepted(resource.Id);
    }

    [RestDeleteById]
    public ActionResult Delete(int id)
    {
        _creatorService.DeleteCreator(id);
        return NoContent();
    }

    [NonAction]
    public void Handle(CreatorAddedEvent message)
    {
        BroadcastResourceChange(ModelAction.Created, message.Creator.ToResource());
    }

    [NonAction]
    public void Handle(CreatorUpdatedEvent message)
    {
        BroadcastResourceChange(ModelAction.Updated, message.Creator.ToResource());
    }

    [NonAction]
    public void Handle(CreatorDeletedEvent message)
    {
        BroadcastResourceChange(ModelAction.Deleted, message.Creator.Id);
    }
}
