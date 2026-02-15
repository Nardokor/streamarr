using System;
using Microsoft.AspNetCore.Mvc;

namespace Streamarr.Http.REST.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RestPostByIdAttribute : HttpPostAttribute
    {
    }
}
