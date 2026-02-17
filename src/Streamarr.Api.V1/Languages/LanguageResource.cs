using Streamarr.Http.REST;

namespace Streamarr.Api.V1.Languages;

public class LanguageResource : RestResource
{
    public required string Name { get; set; }
}
