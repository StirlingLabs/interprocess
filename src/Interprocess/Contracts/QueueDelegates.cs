using System;
using System.Threading;

namespace Cloudtoid.Interprocess
{
    /// <summary>
    /// This function is used to enqueue a message.
    /// A negative value or zero may be returned to abort the operation.
    /// </summary>
    /// <param name="buffer">The writable buffer.</param>
    /// <param name="cancellation">A cancellation token.</param>
    /// <returns>
    /// The amount written into the buffer or a negative
    /// value or zero to abort the operation.
    /// </returns>
    /// <exception cref="Exception">
    /// If an exception is thrown, the operation is aborted.
    /// </exception>
    public delegate long EnqueueZeroCopyFunc(WrappedByteSpan buffer, CancellationToken cancellation);

    /// <summary>
    /// This function is used to dequeue a message.
    /// If false is returned, the message is not dequeued.
    /// </summary>
    /// <param name="buffer">The readable buffer.</param>
    /// <param name="cancellation">A cancellation token.</param>
    /// <returns>
    /// True if the message can be dequeued, otherwise false.
    /// </returns>
    /// <exception cref="Exception">
    /// If an exception is thrown, the operation is aborted.
    /// </exception>
    public delegate bool DequeueZeroCopyFunc(WrappedByteSpan buffer, CancellationToken cancellation);
}
