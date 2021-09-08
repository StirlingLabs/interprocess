using System;
using System.Threading;

namespace Cloudtoid.Interprocess
{
    public interface IPublisher : IDisposable
    {
        bool TryEnqueue(ReadOnlySpan<byte> message);

        /// <summary>
        /// Attempts to enqueue a message.
        /// If the message can not be enqueued immediately,
        /// this message is non-blocking and returns immediately.
        /// The message may not be enqueued if the requested
        /// <paramref name="reserveBytes"/> is unable to be
        /// acquired from the buffer.
        /// The <see cref="Span{T}"/> of <see cref="byte"/> given to
        /// the function is this size.
        /// </summary>
        bool TryEnqueueZeroCopy(long reserveBytes, EnqueueZeroCopyFunc func, CancellationToken cancellation);
    }
}
