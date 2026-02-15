using Streamarr.Common.Messaging;

namespace Streamarr.Core.Profiles.Qualities;

public class QualityProfileUpdatedEvent(int id) : IEvent
{
    public int Id { get; private set; } = id;
}
