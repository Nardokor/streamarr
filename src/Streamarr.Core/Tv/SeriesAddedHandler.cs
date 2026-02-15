using System.Collections.Generic;
using System.Linq;
using Streamarr.Core.Messaging.Commands;
using Streamarr.Core.Messaging.Events;
using Streamarr.Core.Tv.Commands;
using Streamarr.Core.Tv.Events;

namespace Streamarr.Core.Tv
{
    public class SeriesAddedHandler : IHandle<SeriesAddedEvent>,
                                      IHandle<SeriesImportedEvent>
    {
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesAddedHandler(IManageCommandQueue commandQueueManager)
        {
            _commandQueueManager = commandQueueManager;
        }

        public void Handle(SeriesAddedEvent message)
        {
            _commandQueueManager.Push(new RefreshSeriesCommand(new List<int> { message.Series.Id }, true));
        }

        public void Handle(SeriesImportedEvent message)
        {
            _commandQueueManager.PushMany(message.SeriesIds.Select(s => new RefreshSeriesCommand(new List<int> { s }, true)).ToList());
        }
    }
}
