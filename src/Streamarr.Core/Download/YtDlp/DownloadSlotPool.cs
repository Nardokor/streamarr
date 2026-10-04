using System;
using System.Threading;

namespace Streamarr.Core.Download.YtDlp
{
    /// <summary>
    /// Limits concurrent downloads. Unlike a fixed-size semaphore, the limit is re-read while
    /// waiting, so a changed setting applies without a restart, and a wait can be cancelled.
    /// </summary>
    public class DownloadSlotPool
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

        private readonly object _mutex = new object();
        private readonly Func<int> _maxSlots;
        private int _inUse;

        public DownloadSlotPool(Func<int> maxSlots)
        {
            _maxSlots = maxSlots;
        }

        public int MaxSlots => Math.Max(1, _maxSlots());

        public int InUse
        {
            get
            {
                lock (_mutex)
                {
                    return _inUse;
                }
            }
        }

        public IDisposable Acquire(CancellationToken token)
        {
            lock (_mutex)
            {
                // Wake periodically rather than only on release so a raised limit or a
                // cancellation is noticed while every slot is still busy.
                while (_inUse >= MaxSlots)
                {
                    token.ThrowIfCancellationRequested();
                    Monitor.Wait(_mutex, PollInterval);
                }

                token.ThrowIfCancellationRequested();
                _inUse++;
            }

            return new Releaser(this);
        }

        private void Release()
        {
            lock (_mutex)
            {
                _inUse--;
                Monitor.PulseAll(_mutex);
            }
        }

        private sealed class Releaser : IDisposable
        {
            private readonly DownloadSlotPool _pool;
            private int _released;

            public Releaser(DownloadSlotPool pool)
            {
                _pool = pool;
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    _pool.Release();
                }
            }
        }
    }
}
