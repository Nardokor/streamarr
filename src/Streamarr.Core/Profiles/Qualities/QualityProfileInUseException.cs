using System.Net;
using Streamarr.Core.Exceptions;

namespace Streamarr.Core.Profiles.Qualities
{
    public class QualityProfileInUseException : StreamarrClientException
    {
        public QualityProfileInUseException(string name)
            : base(HttpStatusCode.BadRequest, "QualityProfile [{0}] is in use.", name)
        {
        }
    }
}
