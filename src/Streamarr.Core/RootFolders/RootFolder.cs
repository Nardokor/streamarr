using System.Collections.Generic;
using Streamarr.Core.Datastore;

namespace Streamarr.Core.RootFolders
{
    public class RootFolder : ModelBase
    {
        public string Path { get; set; }
        public bool Accessible { get; set; }
        public bool IsEmpty { get; set; }
        public long? FreeSpace { get; set; }
        public long? TotalSpace { get; set; }

        public List<UnmappedFolder> UnmappedFolders { get; set; }
    }
}
