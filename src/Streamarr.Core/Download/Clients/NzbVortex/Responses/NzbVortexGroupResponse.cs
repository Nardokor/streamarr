using System.Collections.Generic;

namespace Streamarr.Core.Download.Clients.NzbVortex.Responses
{
    public class NzbVortexGroupResponse : NzbVortexResponseBase
    {
        public List<NzbVortexGroup> Groups { get; set; }
    }
}
