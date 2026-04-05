using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Streamarr.Core.MetadataSource.YouTube;

namespace Streamarr.Http.Middleware
{
    // When a public Funnel hostname is configured, requests arriving on that host are
    // restricted to the webhook path only. This prevents the full UI and API from being
    // reachable on the public internet — only YouTube's hub can use it.
    public class WebhookOnlyMiddleware
    {
        private const string WebhookPath = "/api/v1/webhook/youtube";

        private readonly RequestDelegate _next;
        private readonly IYoutubeWebSubService _webSubService;

        public WebhookOnlyMiddleware(RequestDelegate next, IYoutubeWebSubService webSubService)
        {
            _next = next;
            _webSubService = webSubService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var webhookHost = _webSubService.GetWebhookHost();

            if (webhookHost != null &&
                context.Request.Host.Host.Equals(webhookHost, StringComparison.OrdinalIgnoreCase) &&
                !context.Request.Path.StartsWithSegments(WebhookPath))
            {
                context.Response.StatusCode = 403;
                return;
            }

            await _next(context);
        }
    }
}
