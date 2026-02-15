using Streamarr.Core.CustomFormats;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.Profiles
{
    public class ProfileFormatItem : IEmbeddedDocument
    {
        public CustomFormat Format { get; set; }
        public int Score { get; set; }
    }
}
