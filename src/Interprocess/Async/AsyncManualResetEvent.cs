using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.Interprocess.Async
{
    /// <summary>
    /// An async-compatible manual-reset event.
    /// </summary>
    [DebuggerDisplay("IsSet = {GetStateForDebugger}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    internal struct AsyncManualResetEvent
    {
        /// <summary>
        /// The object used for synchronization.
        /// </summary>
        private readonly object mutex;

        /// <summary>
        /// The current state of the event.
        /// </summary>
        private TaskCompletionSource<object?> tcs;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncManualResetEvent"/> struct.
        /// Creates an async-compatible manual-reset event.
        /// </summary>
        /// <param name="set">Whether the manual-reset event is initially set or unset.</param>
        public AsyncManualResetEvent(bool set)
        {
            mutex = new();
            tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            if (set)
                tcs.TrySetResult(null);
        }

        [DebuggerNonUserCode]
        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "For the debugger")]
        private bool GetStateForDebugger => tcs.Task.IsCompleted;

        /// <summary>
        /// Whether this event is currently set. This member is seldom used; code using this member has a high possibility of race conditions.
        /// </summary>
        public bool IsSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                lock (mutex) return tcs.Task.IsCompleted;
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task WaitAsync()
        {
            lock (mutex)
            {
                return tcs.Task;
            }
        }

        /// <summary>
        /// Asynchronously waits for this event to be set or for the wait to be canceled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        public Task WaitAsync(CancellationToken cancellationToken)
        {
            var waitTask = WaitAsync();
            return waitTask.IsCompleted ? waitTask : waitTask.WaitAsync(cancellationToken);
        }

        /// <summary>
        /// Synchronously waits for this event to be set. This method may block the calling thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
#pragma warning disable VSTHRD002
            WaitAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        /// <summary>
        /// Synchronously waits for this event to be set. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken cancellationToken)
        {
            var ret = WaitAsync(CancellationToken.None);
            if (ret.IsCompleted)
                return;
#pragma warning disable VSTHRD002
            ret.Wait(cancellationToken);
#pragma warning restore VSTHRD002
        }

        /// <summary>
        /// Sets the event, atomically completing every task returned by <see cref="WaitAsync()"/>. If the event is already set, this method does nothing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set()
        {
            lock (mutex)
            {
                tcs.TrySetResult(null);
            }
        }

        /// <summary>
        /// Resets the event. If the event is already reset, this method does nothing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            lock (mutex)
            {
                if (tcs.Task.IsCompleted)
                    tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly AsyncManualResetEvent mre;

            public DebugView(AsyncManualResetEvent mre)
                => this.mre = mre;

            public bool IsSet => mre.GetStateForDebugger;

            public Task CurrentTask => mre.tcs.Task;
        }
    }
}
