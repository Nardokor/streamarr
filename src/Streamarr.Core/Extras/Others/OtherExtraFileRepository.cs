using Streamarr.Core.Datastore;
using Streamarr.Core.Extras.Files;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Extras.Others
{
    public interface IOtherExtraFileRepository : IExtraFileRepository<OtherExtraFile>
    {
    }

    public class OtherExtraFileRepository : ExtraFileRepository<OtherExtraFile>, IOtherExtraFileRepository
    {
        public OtherExtraFileRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }
    }
}
