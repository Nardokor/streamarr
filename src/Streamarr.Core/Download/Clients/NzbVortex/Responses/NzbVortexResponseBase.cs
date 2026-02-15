using Newtonsoft.Json;
using Streamarr.Core.Download.Clients.NzbVortex.JsonConverters;

namespace Streamarr.Core.Download.Clients.NzbVortex.Responses
{
    public class NzbVortexResponseBase
    {
        [JsonConverter(typeof(NzbVortexResultTypeConverter))]
        public NzbVortexResultType Result { get; set; }
    }
}
