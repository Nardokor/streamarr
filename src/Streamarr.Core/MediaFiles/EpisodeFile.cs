using System;
using System.Collections.Generic;
using Streamarr.Common.Extensions;
using Streamarr.Core.Datastore;
using Streamarr.Core.Languages;
using Streamarr.Core.MediaFiles.MediaInfo;
using Streamarr.Core.Parser.Model;
using Streamarr.Core.Qualities;
using Streamarr.Core.Tv;

namespace Streamarr.Core.MediaFiles
{
    public class EpisodeFile : ModelBase
    {
        public int SeriesId { get; set; }
        public int SeasonNumber { get; set; }
        public string RelativePath { get; set; }
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime DateAdded { get; set; }
        public string OriginalFilePath { get; set; }
        public string SceneName { get; set; }
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public QualityModel Quality { get; set; }
        public IndexerFlags IndexerFlags { get; set; }
        public MediaInfoModel MediaInfo { get; set; }
        public LazyLoaded<List<Episode>> Episodes { get; set; }
        public LazyLoaded<Series> Series { get; set; }
        public List<Language> Languages { get; set; }
        public ReleaseType ReleaseType { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}] {1}", Id, RelativePath);
        }

        public string GetSceneOrFileName()
        {
            if (SceneName.IsNotNullOrWhiteSpace())
            {
                return SceneName;
            }

            if (RelativePath.IsNotNullOrWhiteSpace())
            {
                return System.IO.Path.GetFileNameWithoutExtension(RelativePath);
            }

            if (Path.IsNotNullOrWhiteSpace())
            {
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            }

            return string.Empty;
        }
    }
}
