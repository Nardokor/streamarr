using Streamarr.Common.Messaging;

namespace Streamarr.Core.CustomFormats.Events
{
    public class CustomFormatAddedEvent : IEvent
    {
        public CustomFormatAddedEvent(CustomFormat format)
        {
            CustomFormat = format;
        }

        public CustomFormat CustomFormat { get; private set; }
    }
}
