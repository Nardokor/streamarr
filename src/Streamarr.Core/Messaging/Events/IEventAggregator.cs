using Streamarr.Common.Messaging;

namespace Streamarr.Core.Messaging.Events
{
    public interface IEventAggregator
    {
        void PublishEvent<TEvent>(TEvent @event)
            where TEvent : class,  IEvent;
    }
}
