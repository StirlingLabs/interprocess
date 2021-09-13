﻿using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Cloudtoid.Interprocess.Benchmark
{
    [SimpleJob(RuntimeMoniker.Net50)]
    [MemoryDiagnoser]
    public class QueueExtendedBenchmark
    {
        private static readonly byte[] Message = new byte[50];
        private static readonly byte[] MessageBuffer = new byte[Message.Length];
        private readonly string queueName = InterprocessSemaphore.GenerateName();
#pragma warning disable CS8618
        private IPublisher publisher;
        private ISubscriber subscriber;
#pragma warning restore CS8618

        [GlobalSetup]
        public void Setup()
        {
            var queueFactory = new QueueFactory();
            publisher = queueFactory.CreatePublisher(new QueueOptions(queueName, Path.GetTempPath(), 128));
            subscriber = queueFactory.CreateSubscriber(new QueueOptions(queueName, Path.GetTempPath(), 128));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            subscriber.Dispose();
            publisher.Dispose();
        }

        [Benchmark]
        public ReadOnlyMemory<byte> EnqueueDequeue_LongMessage()
        {
            publisher.TryEnqueue(Message);
            return subscriber.Dequeue(MessageBuffer, default);
        }

        // when a message is wrapped in the circular buffer
        [Benchmark]
        public ReadOnlyMemory<byte> EnqueueDequeue_WrappedMessages()
        {
            publisher.TryEnqueue(Message);
            subscriber.Dequeue(MessageBuffer, default);
            publisher.TryEnqueue(Message);
            return subscriber.Dequeue(MessageBuffer, default);
        }
    }
}
