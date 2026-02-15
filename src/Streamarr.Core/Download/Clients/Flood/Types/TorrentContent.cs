using Newtonsoft.Json;

namespace Streamarr.Core.Download.Clients.Flood.Types
{
    public sealed class TorrentContent
    {
        [JsonProperty(PropertyName = "path")]
        public string Path { get; set; }
    }
}
