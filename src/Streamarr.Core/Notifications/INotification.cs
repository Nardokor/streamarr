using Streamarr.Core.ThingiProvider;

namespace Streamarr.Core.Notifications
{
    public interface INotification : IProvider
    {
        void OnDownload(ContentDownloadedMessage message);
        bool SupportsOnDownload { get; }
    }
}
