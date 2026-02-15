using System;
using Streamarr.Core.Backup;
using Streamarr.Http.REST;

namespace Streamarr.Api.V3.System.Backup
{
    public class BackupResource : RestResource
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public BackupType Type { get; set; }
        public long Size { get; set; }
        public DateTime Time { get; set; }
    }
}
