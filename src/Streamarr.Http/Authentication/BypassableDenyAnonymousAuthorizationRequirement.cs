using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Streamarr.Http.Authentication
{
    public class BypassableDenyAnonymousAuthorizationRequirement : DenyAnonymousAuthorizationRequirement
    {
    }
}
