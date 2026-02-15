using Streamarr.Core.Parser.Model;

namespace Streamarr.Core.Download.Aggregation.Aggregators
{
    public interface IAggregateRemoteEpisode
    {
        RemoteEpisode Aggregate(RemoteEpisode remoteEpisode);
    }
}
