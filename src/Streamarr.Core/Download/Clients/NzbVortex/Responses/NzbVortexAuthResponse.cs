using Newtonsoft.Json;
using Streamarr.Core.Download.Clients.NzbVortex.JsonConverters;

namespace Streamarr.Core.Download.Clients.NzbVortex.Responses
{
    public class NzbVortexAuthResponse : NzbVortexResponseBase
    {
        [JsonConverter(typeof(NzbVortexLoginResultTypeConverter))]
        public NzbVortexLoginResultType LoginResult { get; set; }

        public string SessionId { get; set; }
    }
}
