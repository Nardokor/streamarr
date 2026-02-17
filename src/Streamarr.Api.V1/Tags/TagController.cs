using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Datastore.Events;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.Tags;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;
using Streamarr.SignalR;

namespace Streamarr.Api.V1.Tags;

[V1ApiController]
public class TagController : RestControllerWithSignalR<TagResource, Tag>,
                             IHandle<TagsUpdatedEvent>
{
    private readonly ITagService _tagService;

    public TagController(IBroadcastSignalRMessage signalRBroadcaster,
        ITagService tagService)
        : base(signalRBroadcaster)
    {
        _tagService = tagService;

        SharedValidator.RuleFor(c => c.Label).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$", RegexOptions.IgnoreCase)
            .WithMessage("Allowed characters a-z, 0-9 and -");
    }

    protected override TagResource GetResourceById(int id)
    {
        return _tagService.GetTag(id).ToResource();
    }

    [HttpGet]
    [Produces("application/json")]
    public List<TagResource> GetAll()
    {
        return _tagService.All().ToResource();
    }

    [RestPostById]
    [Consumes("application/json")]
    public ActionResult<TagResource> Create([FromBody] TagResource resource)
    {
        return Created(_tagService.Add(resource.ToModel()).Id);
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<TagResource> Update([FromBody] TagResource resource)
    {
        _tagService.Update(resource.ToModel());
        return Accepted(resource.Id);
    }

    [RestDeleteById]
    public void DeleteTag(int id)
    {
        _tagService.Delete(id);
    }

    [NonAction]
    public void Handle(TagsUpdatedEvent message)
    {
        BroadcastResourceChange(ModelAction.Sync);
    }
}
