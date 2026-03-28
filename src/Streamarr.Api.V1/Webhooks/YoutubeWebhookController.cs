using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Streamarr.Core.Channels;
using Streamarr.Core.Creators.Commands;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.MetadataSource.YouTube;
using Streamarr.Http;

namespace Streamarr.Api.V1.Webhooks;

[AllowAnonymous]
[V1ApiController("webhook/youtube")]
public class YoutubeWebhookController : ControllerBase
{
    private static readonly XNamespace _ytNs = "http://www.youtube.com/xml/schemas/2015";

    private readonly IChannelService _channelService;
    private readonly IYoutubeWebSubService _webSubService;
    private readonly IManageCommandQueue _commandQueue;
    private readonly Logger _logger;

    public YoutubeWebhookController(IChannelService channelService,
                                    IYoutubeWebSubService webSubService,
                                    IManageCommandQueue commandQueue)
    {
        _channelService = channelService;
        _webSubService = webSubService;
        _commandQueue = commandQueue;
        _logger = LogManager.GetLogger(nameof(YoutubeWebhookController));
    }

    // YouTube hub sends a GET to verify the subscription.
    // We echo back hub.challenge as plain text.
    [HttpGet]
    public IActionResult Verify([FromQuery(Name = "hub.challenge")] string? challenge,
                                [FromQuery(Name = "hub.mode")] string? mode)
    {
        if (string.IsNullOrEmpty(challenge) || (mode != "subscribe" && mode != "unsubscribe"))
        {
            return BadRequest();
        }

        return Content(challenge, "text/plain");
    }

    // YouTube hub sends a POST with an Atom feed entry when a channel publishes content.
    [HttpPost]
    public async Task<IActionResult> Notify()
    {
        byte[] bodyBytes;
        using (var ms = new MemoryStream())
        {
            await Request.Body.CopyToAsync(ms);
            bodyBytes = ms.ToArray();
        }

        string? channelId;
        try
        {
            var doc = XDocument.Parse(Encoding.UTF8.GetString(bodyBytes));
            channelId = doc.Descendants(_ytNs + "channelId").FirstOrDefault()?.Value;
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "WebSub: failed to parse notification body");
            return BadRequest();
        }

        if (string.IsNullOrEmpty(channelId))
        {
            _logger.Warn("WebSub: notification body contained no yt:channelId");
            return BadRequest();
        }

        var channel = _channelService.FindByPlatformId(PlatformType.YouTube, channelId);
        if (channel == null)
        {
            _logger.Debug("WebSub: notification for unknown channel {0}, ignoring", channelId);
            return Ok();
        }

        var signature = Request.Headers["X-Hub-Signature"].ToString();
        if (!_webSubService.VerifySignature(channel.WebSubSecret, bodyBytes, signature))
        {
            _logger.Warn("WebSub: HMAC verification failed for channel {0} ({1})", channel.Title, channelId);
            return BadRequest();
        }

        _commandQueue.Push(
            new RefreshCreatorCommand { CreatorId = channel.CreatorId },
            CommandPriority.Normal,
            CommandTrigger.Unspecified);

        _logger.Info(
            "WebSub: notification received for channel {0} ({1}), queued refresh for creator {2}",
            channel.Title,
            channelId,
            channel.CreatorId);

        return Ok();
    }
}
