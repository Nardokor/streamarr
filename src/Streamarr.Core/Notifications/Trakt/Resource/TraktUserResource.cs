using Streamarr.Core.Notifications.Trakt.Resource;

namespace Streamarr.Core.Notifications.Trakt
{
    public class TraktUserResource
    {
        public string Username { get; set; }
        public TraktUserIdsResource Ids { get; set; }
    }
}
