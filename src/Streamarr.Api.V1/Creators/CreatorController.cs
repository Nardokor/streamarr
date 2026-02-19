using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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

    public CreatorController(ICreatorService creatorService,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(signalRBroadcaster)
    {
        _creatorService = creatorService;

        SharedValidator.RuleFor(c => c.Title).NotEmpty();
        SharedValidator.RuleFor(c => c.Path).NotEmpty();
        SharedValidator.RuleFor(c => c.QualityProfileId).GreaterThan(0);
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

    [RestPostById]
    [Consumes("application/json")]
    public ActionResult<CreatorResource> Create([FromBody] CreatorResource resource)
    {
        var creator = _creatorService.AddCreator(resource.ToModel());
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
