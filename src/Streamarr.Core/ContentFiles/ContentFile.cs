using Streamarr.Core.Datastore;
using Streamarr.Core.Qualities;

namespace Streamarr.Core.ContentFiles
{
    public class ContentFile : ModelBase
    {
        public int ContentId { get; set; }
        public string RelativePath { get; set; }
        public long Size { get; set; }
        public QualityModel Quality { get; set; }
    }
}
