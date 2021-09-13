using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Cloudtoid.Interprocess.Benchmark
{
    [SimpleJob(RunStrategy.Throughput, RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class QueueBenchmark
    {
        private const int QueueBytesCapacity = 33554432;
        private const int QueueOpIterations = 256000;
        private static readonly byte[] Message = new byte[128];
        private static readonly Memory<byte> MessageBuffer = new byte[Message.Length];
        private readonly string queueName = InterprocessSemaphore.GenerateName();
#pragma warning disable CS8618
        private IPublisher publisher;
        private ISubscriber subscriber;
        private RandomNumberGenerator rng;
#pragma warning restore CS8618

        [GlobalSetup]
        public void Setup()
        {
            rng = RandomNumberGenerator.Create();
            rng.GetBytes(Message);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            var queueFactory = new QueueFactory();
            publisher = queueFactory.CreatePublisher(new(queueName, Path.GetTempPath(), QueueBytesCapacity));
            subscriber = queueFactory.CreateSubscriber(new(queueName, Path.GetTempPath(), QueueBytesCapacity));
        }

        [IterationCleanup]
        public void Cleanup()
        {
            subscriber.Dispose();
            publisher.Dispose();
        }

        // Expecting that there are NO managed heap allocations.
        [Benchmark(Description = "Enqueue messages", OperationsPerInvoke = QueueOpIterations)]
        public void Enqueue() // this assumes the message has already been composed
        {
            for (var i = 0; i < QueueOpIterations; ++i)
                publisher!.TryEnqueue(Message);
        }

        // Expecting that there are NO managed heap allocations.
        [Benchmark(Description = "Enqueue messages (zero-copy)", OperationsPerInvoke = QueueOpIterations)]
        public void EnqueueZeroCopy() // this also assumes the message was already composed
        {
            for (var i = 0; i < QueueOpIterations; ++i)
                publisher!.TryEnqueueZeroCopy(
                    Message.Length,
                    (buffer, _) =>
                    {
                        /* simulate load?
                        Unsafe.InitBlock(ref buffer.FirstPart[0], 0xFF, checked((uint)buffer.FirstPart.Length));
                        if (buffer.SecondPart.Length > 0)
                            Unsafe.InitBlock(ref buffer.SecondPart[0], 0xFF, checked((uint)buffer.SecondPart.Length));
                        */
                        // ReSharper disable once ConvertToLambdaExpression
                        return buffer.Length;
                    },
                    default);
        }

        // Expecting that there are NO managed heap allocations.
        [Benchmark(Description = "Enqueue messages (zero-copy, func pointers)", OperationsPerInvoke = QueueOpIterations)]
        public unsafe void EnqueueZeroCopyFuncPointer() // this also assumes the message was already composed
        {
            static long Func(object? state, WrappedByteSpan buffer, CancellationToken ct)
                => buffer.Length;

            for (var i = 0; i < QueueOpIterations; ++i)
                publisher!.TryEnqueueZeroCopy(Message.Length, &Func, (object?)null, default);
        }

        [Benchmark(Description = "Enqueue and dequeue messages (buffered allocating)", OperationsPerInvoke = QueueOpIterations)]
        public void EnqueueDequeue_WithResultArrayAllocation()
        {
            for (var i = 0; i < QueueOpIterations; ++i)
            {
                publisher.TryEnqueue(Message);
                subscriber.Dequeue(default);
            }
        }

        // Expecting that there are NO managed heap allocations.
        [Benchmark(Description = "Enqueue and dequeue messages (buffer reuse)", OperationsPerInvoke = QueueOpIterations)]
        public void EnqueueAndDequeue_WithPooledResultArray()
        {
            for (var i = 0; i < QueueOpIterations; ++i)
            {
                publisher.TryEnqueue(Message);
                subscriber.Dequeue(MessageBuffer, default);
            }
        }

        [Benchmark(Description = "Enqueue and dequeue messages (zero-copy)", OperationsPerInvoke = QueueOpIterations)]
        public void ZeroCopyEnqueueDequeue()
        {
            for (var i = 0; i < QueueOpIterations; ++i)
            {
                publisher!.TryEnqueueZeroCopy(
                    Message.Length,
                    (buffer, _) =>
                    {
                        // just need to do something other than directly read from somewhere
                        /* simulate load?
                        rng.GetBytes(buffer.FirstPart);
                        if (buffer.SecondPart.Length > 0)
                            rng.GetBytes(buffer.SecondPart);
                        */
                        Unsafe.InitBlock(ref buffer.FirstPart[0], 0xFF, checked((uint)buffer.FirstPart.Length));
                        if (buffer.SecondPart.Length > 0)
                            Unsafe.InitBlock(ref buffer.SecondPart[0], 0xFF, checked((uint)buffer.SecondPart.Length));

                        return buffer.Length;
                    },
                    default);

                subscriber.DequeueZeroCopy(
                    (buffer, _) =>
                    {
                        // just need to do something to test accessing through the span wrapper
                        // real world may read the entire buffer as a struct or structs
                        var i = 0;
                        for (; i < buffer.Length; i += 16)
                        {
                            var k = buffer[i];
#if NETSTANDARD || NET5_0_OR_GREATER
                            if (Unsafe.IsNullRef(ref k))
                                throw new NullReferenceException();
#else
                            unsafe
                            {
                                if (Unsafe.AsPointer(ref k) == default)
                                    throw new NullReferenceException();
                            }
#endif
                        }

                        return i == 128;
                    },
                    default);
            }
        }

        [Benchmark(Description = "Enqueue and dequeue messages (zero-copy, func pointers)", OperationsPerInvoke = QueueOpIterations)]
        public unsafe void ZeroCopyEnqueueDequeueFuncPointers()
        {
            static long EnqueueFunc(object? state, WrappedByteSpan buffer, CancellationToken cancellation)
            {
                // just need to do something other than directly read from somewhere
                Unsafe.InitBlock(ref buffer.FirstPart[0], 0xFF, checked((uint)buffer.FirstPart.Length));
                if (buffer.SecondPart.Length > 0)
                    Unsafe.InitBlock(ref buffer.SecondPart[0], 0xFF, checked((uint)buffer.SecondPart.Length));

                return buffer.Length;
            }

            static bool DequeueFunc(object? state, WrappedByteSpan buffer, CancellationToken cancellation)
            {
                // just need to do something to test accessing through the span wrapper
                // real world may read the entire buffer as a struct or structs
                var i = 0;
                for (; i < buffer.Length; i += 16)
                {
                    var k = buffer[i];
#if NETSTANDARD || NET5_0_OR_GREATER
                    if (Unsafe.IsNullRef(ref k))
                        throw new NullReferenceException();
#else
                    unsafe
                    {
                        if (Unsafe.AsPointer(ref k) == default)
                            throw new NullReferenceException();
                    }
#endif
                }

                return i == 128;
            }

            for (var i = 0; i < QueueOpIterations; ++i)
            {
                publisher!.TryEnqueueZeroCopy(Message.Length, &EnqueueFunc, (object?)null, default);
                subscriber.DequeueZeroCopy(&DequeueFunc, (object?)null, default);
            }
        }
    }
}
