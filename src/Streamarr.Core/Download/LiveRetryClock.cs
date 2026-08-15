using System;
using System.Threading;

namespace Streamarr.Core.Download
{
    // Small time/sleep abstraction so the live recording supervisor's backoff and retry-window
    // logic can be driven by a fake clock in tests instead of really sleeping.
    public interface ILiveRetryClock
    {
        DateTime UtcNow { get; }

        // Blocks for the given duration, returning early if the token is cancelled.
        void Wait(TimeSpan duration, CancellationToken token);
    }

    public class LiveRetryClock : ILiveRetryClock
    {
        public DateTime UtcNow => DateTime.UtcNow;

        public void Wait(TimeSpan duration, CancellationToken token)
        {
            if (duration <= TimeSpan.Zero)
            {
                return;
            }

            // WaitOne returns true if the token was signalled (cancelled), false on timeout.
            token.WaitHandle.WaitOne(duration);
        }
    }
}
