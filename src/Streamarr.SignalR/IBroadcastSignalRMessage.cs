using System.Threading.Tasks;

namespace Streamarr.SignalR
{
    public interface IBroadcastSignalRMessage
    {
        bool IsConnected { get; }
        Task BroadcastMessage(SignalRMessage message);
    }
}
