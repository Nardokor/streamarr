using System.Collections.Generic;
using Streamarr.Core.CustomFormats;
using Streamarr.Core.Languages;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Qualities;
using Streamarr.Core.Tv;

namespace Streamarr.Core.MediaFiles.EpisodeImport.Manual
{
    public class ManualImportItem
    {
        public string Path { get; set; }
        public string RelativePath { get; set; }
        public string FolderName { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public Series Series { get; set; }
        public int? SeasonNumber { get; set; }
        public List<Episode> Episodes { get; set; }
        public int? EpisodeFileId { get; set; }
        public QualityModel Quality { get; set; } = new();
        public List<Language> Languages { get; set; } = new();
        public string ReleaseGroup { get; set; }
        public string DownloadId { get; set; }
        public List<CustomFormat> CustomFormats { get; set; } = new();
        public int CustomFormatScore { get; set; }
        public int IndexerFlags { get; set; }
        public ReleaseType ReleaseType { get; set; }
        public IEnumerable<ImportRejection> Rejections { get; set; } = new List<ImportRejection>();
    }
}
