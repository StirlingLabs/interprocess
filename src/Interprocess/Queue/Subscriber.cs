using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Cloudtoid.Interprocess.Async;
using Microsoft.Extensions.Logging;

namespace Cloudtoid.Interprocess
{
    internal sealed class Subscriber : Queue, ISubscriber
    {
        private const string SeriousBugMessage
            = "This is unexpected and can be a serious bug. We take a lock on this message "
            + "prior to this point which should ensure that the HeadOffset is left unchanged.";

        private readonly CancellationTokenSource cancellationSource = new();
        private readonly InterprocessSemaphore signal;
        private AsyncCountdownEvent countdownEvent = new(1);

        internal Subscriber(QueueOptions options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory)
        {
            signal = InterprocessSemaphore.Create("C" + options.QueueName);
        }

        protected override void Dispose(bool disposing)
        {
            // drain the Dequeue/TryDequeue requests
            cancellationSource.Cancel();
            countdownEvent.Signal();
            countdownEvent.Wait();

            // There is a potential for a  race condition in *DequeueCore if the cancellationSource.
            // was not cancelled before AddEvent is being called. The sleep here will prevent that.
            Thread.Sleep(millisecondsTimeout: 10);

            if (disposing)
            {
                //countdownEvent.Dispose();
                (signal as IDisposable)?.Dispose();
                cancellationSource.Dispose();
            }

            base.Dispose(disposing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(CancellationToken cancellation, out ReadOnlyMemory<byte> message)
            => TryDequeueCore(default, cancellation, out message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue(Memory<byte> resultBuffer, CancellationToken cancellation, out ReadOnlyMemory<byte> message)
            => TryDequeueCore(resultBuffer, cancellation, out message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> Dequeue(CancellationToken cancellation)
            => DequeueCore(default, cancellation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<byte> Dequeue(Memory<byte> resultBuffer, CancellationToken cancellation)
            => DequeueCore(resultBuffer, cancellation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DequeueZeroCopy(DequeueZeroCopyFunc func, CancellationToken cancellation)
            => DequeueCoreZeroCopy(func, cancellation);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeueZeroCopy(DequeueZeroCopyFunc func, CancellationToken cancellation)
            => TryDequeueZeroCopyImpl(func, cancellation);

        private bool TryDequeueCore(Memory<byte>? resultBuffer, CancellationToken cancellation, out ReadOnlyMemory<byte> message)
        {
            // do NOT reorder the cancellation and the AddCount operation below. See Dispose for more information.
            cancellationSource.ThrowIfCancellationRequested(cancellation);
            countdownEvent.AddCount();

            try
            {
                return TryDequeueImpl(resultBuffer, cancellation, out message);
            }
            finally
            {
                countdownEvent.Signal();
            }
        }

        private ReadOnlyMemory<byte> DequeueCore(Memory<byte>? resultBuffer, CancellationToken cancellation)
        {
            // do NOT reorder the cancellation and the AddCount operation below. See Dispose for more information.
            cancellationSource.ThrowIfCancellationRequested(cancellation);
            countdownEvent.AddCount();

            try
            {
                var i = -5;
                while (true)
                {
                    if (TryDequeueImpl(resultBuffer, cancellation, out var message))
                        return message;

                    if (i > 10)
                        signal.Wait(millisecondsTimeout: 10);
                    else if (i++ > 0)
                        signal.Wait(millisecondsTimeout: i);
                    else
                        Thread.Yield();
                }
            }
            finally
            {
                countdownEvent.Signal();
            }
        }

        private void DequeueCoreZeroCopy(DequeueZeroCopyFunc func, CancellationToken cancellation)
        {
            // do NOT reorder the cancellation and the AddCount operation below. See Dispose for more information.
            cancellationSource.ThrowIfCancellationRequested(cancellation);
            countdownEvent.AddCount();

            try
            {
                var i = -5;
                while (true)
                {
                    if (TryDequeueZeroCopyImpl(func, cancellation))
                        return;

                    if (i > 10)
                        signal.Wait(millisecondsTimeout: 10);
                    else if (i++ > 0)
                        signal.Wait(millisecondsTimeout: i);
                    else
                        Thread.Yield();
                }
            }
            finally
            {
                countdownEvent.Signal();
            }
        }

        private unsafe bool TryDequeueImpl(Memory<byte>? resultBuffer, CancellationToken cancellation, out ReadOnlyMemory<byte> message)
        {
            while (true)
            {
                cancellationSource.ThrowIfCancellationRequested(cancellation);

                message = ReadOnlyMemory<byte>.Empty;
                var header = Header;
                var headOffset = header->HeadOffset;

                if (headOffset == header->TailOffset)
                    return false; // this is an empty queue

                var messageHeader = (MessageHeader*)Buffer.GetPointer(headOffset);

                // take a lock so no other thread can start processing this message
                var actualState = Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.LockedToBeConsumedState, MessageHeader.ReadyToBeConsumedState);

                if (actualState == MessageHeader.AbortedState)
                {
                    // recover from aborted state
                    if (Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.LockedToBeConsumedState, MessageHeader.AbortedState) == MessageHeader.AbortedState)
                    {
                        var bodyLength = messageHeader->BodyLength;
                        var bodyOffset = GetMessageBodyOffset(headOffset);
                        Buffer.Clear(bodyOffset, bodyLength);
                        Buffer.Write(default(MessageHeader), headOffset);
                        var messageLength = GetMessageLength(bodyLength);
                        var newHeadOffset = SafeIncrementMessageOffset(headOffset, messageLength);
                        if (Interlocked.CompareExchange(ref header->HeadOffset, newHeadOffset, headOffset) != headOffset)
                            throw new InvalidOperationException(SeriousBugMessage);
                    }

                    // try to read the next message instead
                    continue;
                }

                // braces here for variable scope
                {
                    if (actualState != MessageHeader.ReadyToBeConsumedState)
                        return false; // some other subscriber got to this message before us

                    // was the header advanced already by another subscriber?
                    if (header->HeadOffset != headOffset)
                    {
                        // revert the lock
                        Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.ReadyToBeConsumedState, MessageHeader.LockedToBeConsumedState);

                        return false;
                    }

                    // read the message body from the queue buffer
                    var bodyLength = messageHeader->BodyLength;
                    var bodyOffset = GetMessageBodyOffset(headOffset);
                    message = Buffer.Read(bodyOffset, bodyLength, resultBuffer);

                    // zero out the message body first
                    Buffer.Clear(bodyOffset, bodyLength);

                    // zero out the message header
                    Buffer.Write(default(MessageHeader), headOffset);

                    // updating the queue header to point the head of the queue to the next available message
                    var messageLength = GetMessageLength(bodyLength);
                    var newHeadOffset = SafeIncrementMessageOffset(headOffset, messageLength);
                    if (Interlocked.CompareExchange(ref header->HeadOffset, newHeadOffset, headOffset) != headOffset)
                        throw new InvalidOperationException(SeriousBugMessage);

                    return true;
                }
            }
        }

        private unsafe bool TryDequeueZeroCopyImpl(DequeueZeroCopyFunc func, CancellationToken cancellation)
        {
            while (true)
            {
                cancellationSource.ThrowIfCancellationRequested(cancellation);

                var header = Header;
                var headOffset = header->HeadOffset;

                if (headOffset == header->TailOffset)
                    return false; // this is an empty queue

                var messageHeader = (MessageHeader*)Buffer.GetPointer(headOffset);

                // take a lock so no other thread can start processing this message
                var actualState = Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.LockedToBeConsumedState, MessageHeader.ReadyToBeConsumedState);

                if (actualState == MessageHeader.AbortedState)
                {
                    // recover from aborted state
                    if (Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.LockedToBeConsumedState, MessageHeader.AbortedState) == MessageHeader.AbortedState)
                    {
                        var bodyLength = messageHeader->BodyLength;
                        var bodyOffset = GetMessageBodyOffset(headOffset);
                        Buffer.Clear(bodyOffset, bodyLength);
                        Buffer.Write(default(MessageHeader), headOffset);
                        var messageLength = GetMessageLength(bodyLength);
                        var newHeadOffset = SafeIncrementMessageOffset(headOffset, messageLength);
                        if (Interlocked.CompareExchange(ref header->HeadOffset, newHeadOffset, headOffset) != headOffset)
                            throw new InvalidOperationException(SeriousBugMessage);
                    }

                    // try to read the next message instead
                    continue;
                }

                // braces here for variable scope
                {
                    if (actualState != MessageHeader.ReadyToBeConsumedState)
                        return false; // some other subscriber got to this message before us

                    // was the header advanced already by another subscriber?
                    if (header->HeadOffset != headOffset)
                    {
                        // revert the lock
                        Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.ReadyToBeConsumedState, MessageHeader.LockedToBeConsumedState);

                        return false;
                    }

                    // read the message body from the queue buffer
                    var bodyLength = messageHeader->BodyLength;
                    var bodyOffset = GetMessageBodyOffset(headOffset);

                    var bugged = false;
                    var success = false;
                    try
                    {
                        var buffer = Buffer.GetWrappedByteSpan(bodyOffset, bodyLength);
                        success = func(buffer, cancellation);
                    }
                    finally
                    {
                        if (!Volatile.Read(ref success))
                        {
                            // revert the lock
                            Interlocked.CompareExchange(ref messageHeader->State, MessageHeader.ReadyToBeConsumedState, MessageHeader.LockedToBeConsumedState);
                        }
                        else
                        {
                            // zero out the message body first
                            Buffer.Clear(bodyOffset, bodyLength);

                            // zero out the message header
                            Buffer.Write(default(MessageHeader), headOffset);

                            // updating the queue header to point the head of the queue to the next available message
                            var messageLength = GetMessageLength(bodyLength);
                            var newHeadOffset = SafeIncrementMessageOffset(headOffset, messageLength);

                            bugged = Interlocked.CompareExchange(ref header->HeadOffset, newHeadOffset, headOffset) != headOffset;
                        }
                    }

                    if (bugged)
                        throw new InvalidOperationException(SeriousBugMessage);

                    return success;
                }
            }
        }
    }
}
