using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Channels;
using Streamarr.Core.Content;
using Streamarr.Core.Creators;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Channels;

[V1ApiController]
public class ChannelController : RestControllerWithSignalR<ChannelResource, Channel>
{
    private readonly IChannelService _channelService;
    private readonly IContentFilterService _contentFilterService;
    private readonly ICreatorService _creatorService;

    public ChannelController(IChannelService channelService,
                             IContentFilterService contentFilterService,
                             ICreatorService creatorService,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(signalRBroadcaster)
    {
        _channelService = channelService;
        _contentFilterService = contentFilterService;
        _creatorService = creatorService;

        SharedValidator.RuleFor(c => c.CreatorId).GreaterThan(0);
        SharedValidator.RuleFor(c => c.PlatformId).NotEmpty();
        SharedValidator.RuleFor(c => c.Title).NotEmpty();
    }

    protected override ChannelResource GetResourceById(int id)
    {
        return _channelService.GetChannel(id).ToResource();
    }

    [HttpGet]
    [Produces("application/json")]
    public List<ChannelResource> GetAll()
    {
        return _channelService.GetAllChannels().ToResource();
    }

    [HttpGet("creator/{creatorId:int}")]
    [Produces("application/json")]
    public List<ChannelResource> GetByCreator(int creatorId)
    {
        return _channelService.GetByCreatorId(creatorId).ToResource();
    }

    [RestPostById]
    [Consumes("application/json")]
    public ActionResult<ChannelResource> Create([FromBody] ChannelResource resource)
    {
        var model = resource.ToModel();
        var creator = _creatorService.GetCreator(model.CreatorId);
        var channel = _channelService.AddChannel(model, creator?.Title ?? string.Empty);
        return Created(channel.Id);
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<ChannelResource> Update([FromBody] ChannelResource resource)
    {
        var channel = resource.ToModel();
        _channelService.UpdateChannel(channel);
        _contentFilterService.ReapplyFilterForChannel(channel);
        return Accepted(resource.Id);
    }

    [RestDeleteById]
    public ActionResult Delete(int id)
    {
        _channelService.DeleteChannel(id);
        return NoContent();
    }
}
