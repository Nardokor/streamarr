using System;
using Streamarr.Core.Datastore;
using Streamarr.Core.Qualities;
using ContentModel = Streamarr.Core.Content.Content;

namespace Streamarr.Core.ContentFiles
{
    public class ContentFile : ModelBase
    {
        // Relationship
        public int ContentId { get; set; }

        // File info
        public string RelativePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }

        // Quality
        public QualityModel Quality { get; set; }

        // Source tracking
        public string OriginalFilePath { get; set; } = string.Empty;

        // Navigation
        public LazyLoaded<ContentModel> Content { get; set; }

        public ContentFile()
        {
            Quality = new QualityModel();
        }
    }
}
