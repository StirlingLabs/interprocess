// Note: Extracts from Stephen Cleary's Nito.AsyncEx, MIT License
// https://github.com/StephenCleary/AsyncEx

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.Interprocess.Async
{
    /// <summary>
    /// An async-compatible countdown event.
    /// </summary>
    [DebuggerDisplay("CurrentCount = {count}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    internal struct AsyncCountdownEvent
    {
        /// <summary>
        /// The object used for synchronization.
        /// </summary>
        private readonly object mutex;

        /// <summary>
        /// The underlying manual-reset event.
        /// </summary>
        private AsyncManualResetEvent mre;

        /// <summary>
        /// The remaining count on this event.
        /// </summary>
        private long count;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncCountdownEvent"/> struct.
        /// Creates an async-compatible countdown event.
        /// </summary>
        /// <param name="count">The number of signals this event will need before it becomes set.</param>
        public AsyncCountdownEvent(long count)
        {
            mutex = new();
            mre = new(count == 0);
            this.count = count;
        }

        /// <summary>
        /// Gets the current number of remaining signals before this event becomes set. This member is seldom used; code using this member has a high possibility of race conditions.
        /// </summary>
        public long CurrentCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (mutex)
                    return count;
            }
        }

        /// <summary>
        /// Asynchronously waits for the count to reach zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task WaitAsync()
            => mre.WaitAsync();

        /// <summary>
        /// Synchronously waits for the count to reach zero. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        public Task WaitAsync(CancellationToken cancellationToken)
            => mre.WaitAsync(cancellationToken);

        /// <summary>
        /// Synchronously waits for the count to reach zero. This method may block the calling thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
            => mre.Wait();

        /// <summary>
        /// Synchronously waits for the count to reach zero. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken cancellationToken)
            => mre.Wait(cancellationToken);

        /// <summary>
        /// Adds the specified value to the current count.
        /// </summary>
        /// <param name="addCount">The amount to change the current count.</param>
        public void AddCount(long addCount)
        {
            if (addCount == 0)
                return;
            lock (mutex)
            {
                var oldCount = count;
                checked
                {
                    count += addCount;
                }

                if (oldCount == 0)
                {
                    mre.Reset();
                }
                else if (count == 0)
                {
                    mre.Set();
                }
                else if ((oldCount < 0 && count > 0) || (oldCount > 0 && count < 0))
                {
                    mre.Set();
                    mre.Reset();
                }
            }
        }

        /// <summary>
        /// Adds one to the current count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddCount()
            => AddCount(1);

        /// <summary>
        /// Subtracts the specified value from the current count.
        /// </summary>
        /// <param name="signalCount">The amount to change the current count.</param>
        public void Signal(long signalCount)
        {
            if (signalCount == 0)
                return;
            lock (mutex)
            {
                var oldCount = count;
                checked
                {
                    count -= signalCount;
                }

                if (oldCount == 0)
                {
                    mre.Reset();
                }
                else if (count == 0)
                {
                    mre.Set();
                }
                else if ((oldCount < 0 && count > 0) || (oldCount > 0 && count < 0))
                {
                    mre.Set();
                    mre.Reset();
                }
            }
        }

        /// <summary>
        /// Subtracts one from the current count.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Signal()
            => Signal(1);

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly AsyncCountdownEvent ce;

            public DebugView(AsyncCountdownEvent ce)
                => this.ce = ce;

            public long CurrentCount => ce.CurrentCount;

            public AsyncManualResetEvent AsyncManualResetEvent => ce.mre;
        }
    }
}
