using Streamarr.Core.MediaFiles;
using Streamarr.Core.Tv;

namespace Streamarr.Core.Notifications
{
    public class EpisodeDeleteMessage
    {
        public string Message { get; set; }
        public Series Series { get; set; }
        public EpisodeFile EpisodeFile { get; set; }

        public DeleteMediaFileReason Reason { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
