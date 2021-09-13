using System;
using System.Buffers;
using System.Threading;

namespace Cloudtoid.Interprocess
{
    public interface ISubscriber : IDisposable
    {
        /// <summary>
        /// Dequeues a message from the queue if the queue is not empty. This is a non-blocking
        /// call and returns immediately.
        /// This overload allocates a <see cref="byte"/> array the size of the message in the
        /// queue and copies the message from the shared memory to it. To avoid this memory
        /// allocation, consider reusing a previously allocated <see cref="byte"/> array with
        /// <see cref="TryDequeue(Memory{byte}, CancellationToken, out ReadOnlyMemory{byte})"/>.
        /// <see cref="T:System.Buffers.ArrayPool`1"/> can be a good way of pooling and
        /// reusing byte arrays.
        /// </summary>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        /// <param name="message">The dequeued message.</param>
        /// <returns>Returns <see langword="false"/> if the queue is empty.</returns>
        bool TryDequeue(
            CancellationToken cancellation,
            out ReadOnlyMemory<byte> message);

        /// <summary>
        /// Dequeues a message from the queue if the queue is not empty. This is a non-blocking
        /// call and returns immediately. This method does not allocated memory and only populates
        /// the <paramref name="buffer"/> that is passed in. Make sure that the buffer is large
        /// enough to receive the entire message, or the message is truncated to fit the buffer.
        /// </summary>
        /// <param name="buffer">The memory buffer that is populated with the message. Make sure
        /// that the buffer is large enough to receive the entire message, or the message is
        /// truncated to fit the buffer.</param>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        /// <param name="message">The dequeued message.</param>
        /// <returns>Returns <see langword="false"/> if the queue is empty.</returns>
        /// <exception cref="InvalidOperationException">
        /// This is unexpected and can be a serious bug. We take a lock on this message
        /// prior to this point which should ensure that the HeadOffset is left unchanged.
        /// </exception>
        bool TryDequeue(
            Memory<byte> buffer,
            CancellationToken cancellation,
            out ReadOnlyMemory<byte> message);

        /// <summary>
        /// Dequeues a message from the queue. If the queue is empty, it *waits* for the
        /// arrival of a new message. This call is blocking until a message is received.
        /// This overload allocates a <see cref="byte"/> array the size of the message in the
        /// queue and copies the message from the shared memory to it. To avoid this memory
        /// allocation, consider reusing a previously allocated <see cref="byte"/> array with
        /// <see cref="Dequeue(Memory{byte}, CancellationToken)"/>.
        /// <see cref="T:System.Buffers.ArrayPool`1"/> can be a good way of pooling and
        /// reusing byte arrays.
        /// </summary>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        ReadOnlyMemory<byte> Dequeue(CancellationToken cancellation);

        /// <summary>
        /// Dequeues a message from the queue. If the queue is empty, it *waits* for the
        /// arrival of a new message. This call is blocking until a message is received.
        /// This method does not allocated memory and only populates
        /// the <paramref name="buffer"/> that is passed in. Make sure that the buffer is large
        /// enough to receive the entire message, or the message is truncated to fit the buffer.
        /// </summary>
        /// <param name="buffer">The memory buffer that is populated with the message. Make sure
        /// that the buffer is large enough to receive the entire message, or the message is
        /// truncated to fit the buffer.</param>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        ReadOnlyMemory<byte> Dequeue(
            Memory<byte> buffer,
            CancellationToken cancellation);

        /// <summary>
        /// Dequeues a message from the queue. If the queue is empty, it *waits* for the
        /// arrival of a new message. This call is blocking until a message is received.
        /// This method does not allocated memory and blocks until the
        /// <paramref name="func"/> that is passed in completes.
        /// </summary>
        /// <param name="func">This function is called when the message is ready to be
        /// read. It blocks normal operation of the queue while acting.</param>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        void DequeueZeroCopy(
            DequeueZeroCopyFunc func,
            CancellationToken cancellation);

        /// <summary>
        /// Dequeues a message from the queue. If the queue is empty, it *waits* for the
        /// arrival of a new message. This call is blocking until a message is received.
        /// This method does not allocated memory and blocks until the
        /// <paramref name="func"/> that is passed in completes.
        /// </summary>
        /// <param name="func">This function is called when the message is ready to be
        /// read. It blocks normal operation of the queue while acting. See <see cref="DequeueZeroCopyFunc"/> for details.</param>
        /// <param name="state">A user supplied state context to pass along to <paramref name="func"/>.</param>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        unsafe void DequeueZeroCopy<TState>(
            delegate* managed<TState, WrappedByteSpan, CancellationToken, bool> func,
            TState state,
            CancellationToken cancellation);

        /// <summary>
        /// Dequeues a message from the queue if the queue is not empty.
        /// If a message is not ready this is a non-blocking
        /// call and returns immediately.
        /// This method does not allocated memory and blocks until the
        /// <paramref name="func"/> that is passed in completes.
        /// </summary>
        /// <param name="func">This action is called when the message is ready to be
        /// read. It blocks normal operation of the queue while acting.</param>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>True on success, otherwise false.</returns>
        /// <exception cref="InvalidOperationException">
        /// This is unexpected and can be a serious bug. We take a lock on this message
        /// prior to this point which should ensure that the HeadOffset is left unchanged.
        /// </exception>
        bool TryDequeueZeroCopy(
            DequeueZeroCopyFunc func,
            CancellationToken cancellation);

        /// <summary>
        /// Dequeues a message from the queue if the queue is not empty.
        /// If a message is not ready this is a non-blocking
        /// call and returns immediately.
        /// This method does not allocated memory and blocks until the
        /// <paramref name="func"/> that is passed in completes.
        /// </summary>
        /// <param name="func">This action is called when the message is ready to be
        /// read. It blocks normal operation of the queue while acting. See <see cref="DequeueZeroCopyFunc"/> for details.</param>
        /// <param name="state">A user supplied state context to pass along to <paramref name="func"/>.</param>
        /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>True on success, otherwise false.</returns>
        /// <exception cref="InvalidOperationException">
        /// This is unexpected and can be a serious bug. We take a lock on this message
        /// prior to this point which should ensure that the HeadOffset is left unchanged.
        /// </exception>
        unsafe bool TryDequeueZeroCopy<TState>(
            delegate* managed<TState, WrappedByteSpan, CancellationToken, bool> func,
            TState state,
            CancellationToken cancellation);
    }
}
