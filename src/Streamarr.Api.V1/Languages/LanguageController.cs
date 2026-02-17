using Microsoft.AspNetCore.Mvc;
using Streamarr.Core.Languages;
using Streamarr.Http;

namespace Streamarr.Api.V1.Languages;

[V1ApiController]
public class LanguageController : Controller
{
    [HttpGet("/api/v1/language")]
    [Produces("application/json")]
    public List<LanguageResource> GetAll()
    {
        return Language.All.Select(l => new LanguageResource
        {
            Id = l.Id,
            Name = l.Name
        }).ToList();
    }
}
