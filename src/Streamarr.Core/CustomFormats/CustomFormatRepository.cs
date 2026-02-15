using Streamarr.Core.Datastore;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.CustomFormats
{
    public interface ICustomFormatRepository : IBasicRepository<CustomFormat>
    {
    }

    public class CustomFormatRepository : BasicRepository<CustomFormat>, ICustomFormatRepository
    {
        public CustomFormatRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
