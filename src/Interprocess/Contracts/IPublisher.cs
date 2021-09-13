using System;
using System.Threading;

namespace Cloudtoid.Interprocess
{
    public interface IPublisher : IDisposable
    {
        /// <summary>
        /// Attempts to enqueue a message.
        /// This is non-blocking and returns immediately.
        /// The message may not be enqueued if the requested
        /// <paramref name="message"/> size is unable to be
        /// acquired from the buffer.
        /// </summary>
        bool TryEnqueue(ReadOnlySpan<byte> message);

        /// <summary>
        /// Attempts to enqueue a message.
        /// If the message can not be enqueued immediately,
        /// this is non-blocking and returns immediately.
        /// The message may not be enqueued if the requested
        /// <paramref name="reserveBytes"/> is unable to be
        /// acquired from the buffer.
        /// The <see cref="Span{T}"/> of <see cref="byte"/> given to
        /// the function is this size.
        /// </summary>
        bool TryEnqueueZeroCopy(long reserveBytes, EnqueueZeroCopyFunc func, CancellationToken cancellation);

        /// <summary>
        /// Attempts to enqueue a message.
        /// If the message can not be enqueued immediately,
        /// this is non-blocking and returns immediately.
        /// The message may not be enqueued if the requested
        /// <paramref name="reserveBytes"/> is unable to be
        /// acquired from the buffer.
        /// The <see cref="Span{T}"/> of <see cref="byte"/> given to
        /// the function is this size.
        /// </summary>
        unsafe bool TryEnqueueZeroCopy<TState>(long reserveBytes, delegate* managed<TState, WrappedByteSpan, CancellationToken, long> func, TState state, CancellationToken cancellation);
    }
}
