using NLog;
using Streamarr.Core.Messaging.Events;

namespace Streamarr.Core.Notifications
{
    public class NotificationService :
        IHandle<ContentGrabbedEvent>,
        IHandle<ContentDownloadedEvent>,
        IHandle<LiveStreamStartedEvent>,
        IHandle<LiveStreamEndedEvent>,
        IHandle<ChannelAddedEvent>
    {
        private readonly NotificationFactory _notificationFactory;
        private readonly Logger _logger;

        public NotificationService(NotificationFactory notificationFactory, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _logger = logger;
        }

        public void Handle(ContentGrabbedEvent message)
        {
            Dispatch(
                def => def.OnGrab,
                prov => prov.SupportsOnGrab,
                prov => prov.OnGrab(message.Message),
                "OnGrab");
        }

        public void Handle(ContentDownloadedEvent message)
        {
            Dispatch(
                def => def.OnDownload,
                prov => prov.SupportsOnDownload,
                prov => prov.OnDownload(message.Message),
                "OnDownload");
        }

        public void Handle(LiveStreamStartedEvent message)
        {
            Dispatch(
                def => def.OnLiveStreamStart,
                prov => prov.SupportsOnLiveStreamStart,
                prov => prov.OnLiveStreamStart(message.Message),
                "OnLiveStreamStart");
        }

        public void Handle(LiveStreamEndedEvent message)
        {
            Dispatch(
                def => def.OnLiveStreamEnd,
                prov => prov.SupportsOnLiveStreamEnd,
                prov => prov.OnLiveStreamEnd(message.Message),
                "OnLiveStreamEnd");
        }

        public void Handle(ChannelAddedEvent message)
        {
            Dispatch(
                def => def.OnChannelAdded,
                prov => prov.SupportsOnChannelAdded,
                prov => prov.OnChannelAdded(message.Message),
                "OnChannelAdded");
        }

        private void Dispatch(
            System.Func<NotificationDefinition, bool> isEnabled,
            System.Func<INotification, bool> isSupported,
            System.Action<INotification> send,
            string eventName)
        {
            var definitions = _notificationFactory.All();

            foreach (var definition in definitions)
            {
                if (!definition.Enable || !isEnabled(definition))
                {
                    continue;
                }

                var provider = _notificationFactory.GetInstance(definition);

                if (!isSupported(provider))
                {
                    continue;
                }

                try
                {
                    send(provider);
                }
                catch (System.Exception ex)
                {
                    _logger.Error(ex, "Error sending {0} notification to {1}", eventName, definition.Name);
                }
            }
        }
    }
}
