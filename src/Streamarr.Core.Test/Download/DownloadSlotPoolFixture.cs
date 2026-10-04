using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Streamarr.Core.Download.YtDlp;

namespace Streamarr.Core.Test.Download
{
    [TestFixture]
    public class DownloadSlotPoolFixture
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Test]
        public void should_grant_slots_up_to_the_limit()
        {
            var pool = new DownloadSlotPool(() => 2);

            using (pool.Acquire(CancellationToken.None))
            using (pool.Acquire(CancellationToken.None))
            {
                pool.InUse.Should().Be(2);
            }

            pool.InUse.Should().Be(0);
        }

        [Test]
        public void should_block_until_a_slot_is_released()
        {
            var pool = new DownloadSlotPool(() => 1);
            var held = pool.Acquire(CancellationToken.None);

            var waiter = Task.Run(() => pool.Acquire(CancellationToken.None));

            waiter.Wait(TimeSpan.FromMilliseconds(200)).Should().BeFalse();

            held.Dispose();

            waiter.Wait(Timeout).Should().BeTrue();
            pool.InUse.Should().Be(1);
        }

        [Test]
        public void should_release_only_once_when_disposed_twice()
        {
            var pool = new DownloadSlotPool(() => 2);
            var first = pool.Acquire(CancellationToken.None);
            pool.Acquire(CancellationToken.None);

            first.Dispose();
            first.Dispose();

            pool.InUse.Should().Be(1);
        }

        [Test]
        public void should_stop_waiting_when_cancelled()
        {
            var pool = new DownloadSlotPool(() => 1);
            pool.Acquire(CancellationToken.None);

            using var cts = new CancellationTokenSource();
            var waiter = Task.Run(() => pool.Acquire(cts.Token));

            cts.Cancel();

            Action wait = () => waiter.Wait(Timeout);
            wait.Should().Throw<AggregateException>().WithInnerException<OperationCanceledException>();
            pool.InUse.Should().Be(1);
        }

        [Test]
        public void should_pick_up_a_raised_limit_without_a_release()
        {
            var max = 1;
            var pool = new DownloadSlotPool(() => Volatile.Read(ref max));
            pool.Acquire(CancellationToken.None);

            var waiter = Task.Run(() => pool.Acquire(CancellationToken.None));
            waiter.Wait(TimeSpan.FromMilliseconds(200)).Should().BeFalse();

            Volatile.Write(ref max, 2);

            waiter.Wait(Timeout).Should().BeTrue();
            pool.InUse.Should().Be(2);
        }

        [Test]
        public void should_treat_a_limit_below_one_as_one()
        {
            var pool = new DownloadSlotPool(() => 0);

            pool.MaxSlots.Should().Be(1);
            using (pool.Acquire(CancellationToken.None))
            {
                pool.InUse.Should().Be(1);
            }
        }
    }
}
