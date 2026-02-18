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
            return Query(c => c.ChannelId == channelId && c.ContentFileId == 0);
        }
    }
}
