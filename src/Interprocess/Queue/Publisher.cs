using System;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Cloudtoid.Interprocess
{
    internal sealed class Publisher : Queue, IPublisher
    {
        private const string BadStateRequiresCrash = "Publishing to the shared memory queue failed leaving the queue in a bad state. " +
            "The only option is to crash the application.";
        private readonly InterprocessSemaphore signal;

        internal Publisher(QueueOptions options, ILoggerFactory loggerFactory)
            : base(options, loggerFactory)
        {
            signal = InterprocessSemaphore.Create("C" + options.QueueName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                (signal as IDisposable)?.Dispose();

            base.Dispose(disposing);
        }

        public unsafe bool TryEnqueue(ReadOnlySpan<byte> message)
        {
            var bodyLength = message.Length;
            while (true)
            {
                var header = *Header;
                var tailOffset = header.TailOffset;

                var messageLength = GetMessageLength(bodyLength);
                var capacity = Buffer.Capacity - tailOffset + header.HeadOffset;
                if (messageLength > capacity)
                    return false;

                var newTailOffset = SafeIncrementMessageOffset(tailOffset, messageLength);

                // try to atomically update the tail-offset that is stored in the queue header
                var currentTailOffset = (long*)Header + 1;
                if (Interlocked.CompareExchange(ref *currentTailOffset, newTailOffset, tailOffset) != tailOffset)
                    continue;

                var success = false;
                try
                {
                    // write the message body
                    Buffer.Write(message, GetMessageBodyOffset(tailOffset));
                    success = true;
                }
                catch
                {
                    success = false;
                    throw;
                }
                finally
                {
                    try
                    {
                        // write the message header
                        var headerState = Volatile.Read(ref success) ? MessageHeader.ReadyToBeConsumedState : MessageHeader.AbortedState;
                        Buffer.Write(new MessageHeader(headerState, bodyLength), tailOffset);

                        // signal the next receiver that there is a new message in the queue
                        signal.Release();
                    }
                    catch (Exception ex)
                    {
                        Environment.FailFast(BadStateRequiresCrash, ex);
                    }
                }

                return true;
            }
        }

        public unsafe bool TryEnqueueZeroCopy(long reserveBytes, EnqueueZeroCopyFunc func, CancellationToken cancellation)
        {
            var bodyLength = reserveBytes;
            while (true)
            {
                var header = *Header;
                var tailOffset = header.TailOffset;

                var messageLength = GetMessageLength(bodyLength);
                var capacity = Buffer.Capacity - tailOffset + header.HeadOffset;
                if (messageLength > capacity)
                    return false;

                var newTailOffset = SafeIncrementMessageOffset(tailOffset, messageLength);

                // try to atomically update the tail-offset that is stored in the queue header
                var currentTailOffset = (long*)Header + 1;
                if (Interlocked.CompareExchange(ref *currentTailOffset, newTailOffset, tailOffset) != tailOffset)
                    continue;

                var success = false;
                long written = 0;
                try
                {
                    // write the message body
                    var buffer = Buffer.GetWrappedByteSpan(GetMessageBodyOffset(tailOffset), reserveBytes);
                    written = func(buffer, cancellation);
                    success = written > 0;
                }
                catch
                {
                    success = false;
                }
                finally
                {
                    try
                    {
                        // write the message header
                        //success = Volatile.Read(ref written) > 0;
                        var headerState = success
                            ? MessageHeader.ReadyToBeConsumedState
                            : MessageHeader.AbortedState;
                        Buffer.Write(
                            new MessageHeader(headerState, success
                            ? checked((int)written)
                            : checked((int)reserveBytes)),
                            tailOffset);

                        // signal the next receiver that there is a new message in the queue
                        signal.Release();
                    }
                    catch (Exception ex)
                    {
                        Environment.FailFast(
                            BadStateRequiresCrash,
                            ex);
                    }
                }

                return success; // Volatile.Read(ref success);
            }
        }

        public unsafe bool TryEnqueueZeroCopy<TState>(long reserveBytes, delegate*<TState, WrappedByteSpan, CancellationToken, long> func, TState state, CancellationToken cancellation)
        {
            var bodyLength = reserveBytes;
            while (true)
            {
                var header = *Header;
                var tailOffset = header.TailOffset;

                var messageLength = GetMessageLength(bodyLength);
                var capacity = Buffer.Capacity - tailOffset + header.HeadOffset;
                if (messageLength > capacity)
                    return false;

                var newTailOffset = SafeIncrementMessageOffset(tailOffset, messageLength);

                // try to atomically update the tail-offset that is stored in the queue header
                var currentTailOffset = (long*)Header + 1;
                if (Interlocked.CompareExchange(ref *currentTailOffset, newTailOffset, tailOffset) != tailOffset)
                    continue;

                var success = false;
                long written = 0;
                try
                {
                    // write the message body
                    var buffer = Buffer.GetWrappedByteSpan(GetMessageBodyOffset(tailOffset), reserveBytes);
                    written = func(state, buffer, cancellation);
                    success = written > 0;
                }
                catch
                {
                    success = false;
                }
                finally
                {
                    try
                    {
                        // write the message header
                        var headerState = success
                            ? MessageHeader.ReadyToBeConsumedState
                            : MessageHeader.AbortedState;
                        Buffer.Write(
                            new MessageHeader(headerState, success
                            ? checked((int)written)
                            : checked((int)reserveBytes)),
                            tailOffset);

                        // signal the next receiver that there is a new message in the queue
                        signal.Release();
                    }
                    catch (Exception ex)
                    {
                        Environment.FailFast(
                            BadStateRequiresCrash,
                            ex);
                    }
                }

                return true;
            }
        }
    }
}
