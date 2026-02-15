using System.Collections.Generic;

namespace Streamarr.Core.Notifications.Xbmc.Model
{
    public class ActivePlayersResult
    {
        public string Id { get; set; }
        public string JsonRpc { get; set; }
        public List<ActivePlayer> Result { get; set; }
    }
}
