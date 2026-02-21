using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Channels
{
    public interface IChannelRepository : IBasicRepository<Channel>
    {
        List<Channel> GetByCreatorId(int creatorId);
        Channel FindByPlatformId(PlatformType platform, string platformId);
    }

    public class ChannelRepository : BasicRepository<Channel>, IChannelRepository
    {
        public ChannelRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        protected override bool PublishModelEvents => true;

        public List<Channel> GetByCreatorId(int creatorId)
        {
            return Query(c => c.CreatorId == creatorId);
        }

        public Channel FindByPlatformId(PlatformType platform, string platformId)
        {
            return Query(c => c.Platform == platform && c.PlatformId == platformId).FirstOrDefault();
        }
    }
}
