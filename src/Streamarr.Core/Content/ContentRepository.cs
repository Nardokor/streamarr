using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Content
{
    public interface IContentRepository : IBasicRepository<Content>
    {
        List<Content> GetByChannelId(int channelId);
        Content FindByPlatformContentId(int channelId, string platformContentId);
        List<Content> GetWithoutFiles(int channelId);
        List<Content> GetAllMissing();
        List<Content> GetAllDownloaded();
        List<Content> GetAllLiveNow();
        List<Content> GetAllRecording();
        List<Content> GetAllWanted();
    }

    public class ContentRepository : BasicRepository<Content>, IContentRepository
    {
        public ContentRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override bool PublishModelEvents => true;

        public List<Content> GetByChannelId(int channelId)
        {
            return Query(c => c.ChannelId == channelId);
        }

        public Content FindByPlatformContentId(int channelId, string platformContentId)
        {
            return Query(c => c.ChannelId == channelId && c.PlatformContentId == platformContentId).SingleOrDefault();
        }

        public List<Content> GetWithoutFiles(int channelId)
        {
            return Query(c => c.ChannelId == channelId && c.ContentFileId == 0 && c.Status == ContentStatus.Missing);
        }

        public List<Content> GetAllMissing()
        {
            return Query(c => c.Monitored && c.Status == ContentStatus.Missing && c.ContentType != ContentType.Upcoming);
        }

        public List<Content> GetAllLiveNow()
        {
            // ContentType transitions from Live → Vod when a stream ends, so any
            // remaining Live item is genuinely airing right now regardless of status.
            return Query(c => c.ContentType == ContentType.Live);
        }

        public List<Content> GetAllDownloaded()
        {
            return Query(c => c.Status == ContentStatus.Downloaded);
        }

        public List<Content> GetAllRecording()
        {
            return Query(c => c.Status == ContentStatus.Recording);
        }

        public List<Content> GetAllWanted()
        {
            return Query(c => c.Monitored && c.Status != ContentStatus.Unwanted);
        }
    }
}
