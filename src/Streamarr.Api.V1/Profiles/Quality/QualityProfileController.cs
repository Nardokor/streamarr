using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Profiles.Qualities;
using Streamarr.Http;
using Streamarr.Http.REST;
using Streamarr.Http.REST.Attributes;

namespace Streamarr.Api.V1.Profiles.Quality;

[V1ApiController]
public class QualityProfileController : RestController<QualityProfileResource>
{
    private readonly IQualityProfileService _profileService;

    public QualityProfileController(IQualityProfileService profileService)
    {
        _profileService = profileService;
        SharedValidator.RuleFor(c => c.Name).NotEmpty();
        SharedValidator.RuleFor(c => c.Cutoff).ValidCutoff();
        SharedValidator.RuleFor(c => c.Items).ValidItems();
    }

    [RestPostById]
    [Consumes("application/json")]
    public ActionResult<QualityProfileResource> Create([FromBody] QualityProfileResource resource)
    {
        var model = resource.ToModel();
        model = _profileService.Add(model);
        return Created(model.Id);
    }

    [RestDeleteById]
    public ActionResult DeleteProfile(int id)
    {
        _profileService.Delete(id);

        return NoContent();
    }

    [RestPutById]
    [Consumes("application/json")]
    public ActionResult<QualityProfileResource> Update([FromBody] QualityProfileResource resource)
    {
        var model = resource.ToModel();

        _profileService.Update(model);

        return Accepted(model.Id);
    }

    protected override QualityProfileResource GetResourceById(int id)
    {
        return _profileService.Get(id).ToResource();
    }

    [HttpGet]
    [Produces("application/json")]
    public List<QualityProfileResource> GetAll()
    {
        return _profileService.All().ToResource();
    }
}
