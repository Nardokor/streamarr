using System.Linq;
using System.Text;
using Streamarr.Core.Channels;
using Streamarr.Core.Localization;

namespace Streamarr.Core.HealthCheck.Checks
{
    public class DuplicateChannelHealthCheck : HealthCheckBase
    {
        private readonly IChannelService _channelService;

        public DuplicateChannelHealthCheck(IChannelService channelService,
                                           ILocalizationService localizationService)
            : base(localizationService)
        {
            _channelService = channelService;
        }

        public override HealthCheck Check()
        {
            var duplicates = _channelService.GetAllChannels()
                .GroupBy(c => new { c.Platform, c.PlatformId })
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            var sb = new StringBuilder();
            sb.Append("Duplicate channel entries detected. ");
            sb.Append("This can cause WebSub HMAC failures and missed notifications. ");
            sb.Append("Remove the duplicate(s) via Settings → Channels. ");
            sb.Append("Affected: ");
            sb.Append(string.Join(", ", duplicates.Select(g => $"{g.First().Title} ({g.Key.PlatformId}, {g.Count()} entries)")));

            return new HealthCheck(
                GetType(),
                HealthCheckResult.Warning,
                HealthCheckReason.DuplicateChannels,
                sb.ToString());
        }
    }
}
