using System.Collections.Generic;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.Tv.Commands;
using Streamarr.Core.Tv.Events;

namespace Streamarr.Core.Tv
{
    public class SeriesEditedService : IHandle<SeriesEditedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesEditedService(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(SeriesEditedEvent message)
        {
            if (message.Series.SeriesType != message.OldSeries.SeriesType)
            {
                _commandQueueManager.Push(new RefreshSeriesCommand(new List<int> { message.Series.Id }, false));
            }
        }
    }
}
