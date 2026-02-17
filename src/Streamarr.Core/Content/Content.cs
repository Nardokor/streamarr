using System;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Content
{
    public class Content : ModelBase
    {
        public int ChannelId { get; set; }
        public string Title { get; set; }
        public string PlatformContentId { get; set; }
        public DateTime? AirDateUtc { get; set; }
        public bool Monitored { get; set; }
    }
}
