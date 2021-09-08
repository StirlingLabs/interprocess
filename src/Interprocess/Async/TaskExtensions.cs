// Note: Extracts from Stephen Cleary's Nito.AsyncEx, MIT License
// https://github.com/StephenCleary/AsyncEx
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudtoid.Interprocess.Async
{
    /// <summary>
    /// Provides extension methods for the <see cref="Task"/> and <see cref="Task{T}"/> types.
    /// </summary>
    internal static class TaskExtensions
    {
        /// <summary>
        /// Asynchronously waits for the task to complete, or for the cancellation token to be canceled.
        /// </summary>
        /// <param name="this">The task to wait for. May not be <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token that cancels the wait.</param>
        public static Task WaitAsync(this Task @this, CancellationToken cancellationToken)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

#pragma warning disable VSTHRD003
            return !cancellationToken.CanBeCanceled
                ? @this
                : cancellationToken.IsCancellationRequested
                    ? Task.FromCanceled(cancellationToken)
                    : DoWaitAsync(@this, cancellationToken);
#pragma warning restore VSTHRD003
        }

        private static async Task DoWaitAsync(Task task, CancellationToken cancellationToken)
        {
            using var cancelTaskSource = new CancellationTokenTaskSource<object>(cancellationToken);
            await (await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously waits for the task to complete, or for the cancellation token to be canceled.
        /// </summary>
        /// <typeparam name="TResult">The type of the task result.</typeparam>
        /// <param name="this">The task to wait for. May not be <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token that cancels the wait.</param>
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> @this, CancellationToken cancellationToken)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));

#pragma warning disable VSTHRD003
            return !cancellationToken.CanBeCanceled
                ? @this
                : cancellationToken.IsCancellationRequested
                    ? Task.FromCanceled<TResult>(cancellationToken)
                    : DoWaitAsync(@this, cancellationToken);
#pragma warning restore VSTHRD003
        }

        private static async Task<TResult> DoWaitAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            using var cancelTaskSource = new CancellationTokenTaskSource<TResult>(cancellationToken);
            return await (await Task.WhenAny(task, cancelTaskSource.Task).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}
