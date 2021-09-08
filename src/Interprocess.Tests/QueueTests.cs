using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Cloudtoid.Interprocess.Tests
{
    [Collection("SharedNamespaceTests")]
    public class QueueTests : LoggingTestBase, IClassFixture<UniquePathFixture>
    {
        private static readonly byte[] ByteArray1 = new byte[] { 100, };
        private static readonly byte[] ByteArray2 = new byte[] { 100, 110 };
        private static readonly byte[] ByteArray3 = new byte[] { 100, 110, 120 };
        private static readonly byte[] ByteArray50 = Enumerable.Range(1, 50).Select(i => (byte)i).ToArray();
        private readonly UniquePathFixture fixture;
        private readonly QueueFactory queueFactory;
        private readonly string queueName;

        public QueueTests(
            UniquePathFixture fixture,
            ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
        {
            DebugContext.TestOutputHelper = testOutputHelper;
            this.fixture = fixture;
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new XunitLoggerProvider(testOutputHelper));
            queueFactory = new QueueFactory(loggerFactory);
            queueName = InterprocessSemaphore.GenerateName();
            Output.WriteLine($"Queue Name: {queueName}");
        }

        [Fact]
        public void Sample()
            => BeforeAfterTest(() =>
            {
                var message = new byte[] { 1, 2, 3 };
                var messageBuffer = new byte[3];
                CancellationToken cancellationToken = default;

                var factory = new QueueFactory();
                var options = new QueueOptions(
                    queueName: queueName,
                    bytesCapacity: 1024 * 1024);

                using var publisher = factory.CreatePublisher(options);
                publisher.TryEnqueue(message);

                options = new QueueOptions(
                    queueName: queueName,
                    bytesCapacity: 1024 * 1024);

                using var subscriber = factory.CreateSubscriber(options);
                subscriber.TryDequeue(messageBuffer, cancellationToken, out var msg);

                msg.ToArray().Should().BeEquivalentTo(message);
            });

        [Fact]
        public void DependencyInjectionSample()
            => BeforeAfterTest(() =>
            {
                var message = new byte[] { 1, 2, 3 };
                var messageBuffer = new byte[3];
                CancellationToken cancellationToken = default;
                var services = new ServiceCollection();

                services
                    .AddInterprocessQueue() // adding the queue related components
                    .AddLogging(); // optionally, we can enable logging

                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetRequiredService<IQueueFactory>();

                var options = new QueueOptions(
                    queueName: queueName,
                    bytesCapacity: 1024 * 1024);

                using var publisher = factory.CreatePublisher(options);
                publisher.TryEnqueue(message);

                options = new QueueOptions(
                    queueName: queueName,
                    bytesCapacity: 1024 * 1024);

                using var subscriber = factory.CreateSubscriber(options);
                subscriber.TryDequeue(messageBuffer, cancellationToken, out var msg);

                msg.ToArray().Should().BeEquivalentTo(message);
            });

        [Fact]
        public void CanEnqueueAndDequeue()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(40);
                using var s = CreateSubscriber(40);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                var message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                message = s.Dequeue(new byte[5], default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);
            });

        [Fact]
        public void CanEnqueueAndDequeueChannel()
            => BeforeAfterTest(() =>
            {
                using var server = CreateChannel(40);
                var p = server.Publisher;

                using var client = ConsumeChannel(40);
                var s = client.Subscriber;

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                var message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                message = s.Dequeue(new byte[5], default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);

                p = client.Publisher;
                s = server.Subscriber;

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                message = s.Dequeue(new byte[5], default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);
            });

        [Fact]
        public void CanEnqueueZeroCopyAndDequeue()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(40);
                using var s = CreateSubscriber(40);

                p.TryEnqueueZeroCopy(
                    ByteArray3.Length,
                    (buffer, _) =>
                    {
                        buffer.Write(ByteArray3);
                        return ByteArray3.Length;
                    },
                    default).Should().BeTrue();
                var message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueueZeroCopy(
                    ByteArray3.Length,
                    (buffer, _) =>
                    {
                        buffer.Write(ByteArray3);
                        return ByteArray3.Length;
                    },
                    default).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray3);

                p.TryEnqueueZeroCopy(
                    ByteArray2.Length,
                    (buffer, _) =>
                    {
                        buffer.Write(ByteArray2);
                        return ByteArray2.Length;
                    },
                    default).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);

                p.TryEnqueueZeroCopy(
                    ByteArray2.Length,
                    (buffer, _) =>
                    {
                        buffer.Write(ByteArray2);
                        return ByteArray2.Length;
                    },
                    default).Should().BeTrue();
                message = s.Dequeue(new byte[5], default);
                message.ToArray().Should().BeEquivalentTo(ByteArray2);
            });

        [Fact]
        public void CanEnqueueAndDequeueZeroCopy()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(40);
                using var s = CreateSubscriber(40);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                s.DequeueZeroCopy(
                    (msg, _) =>
                    {
                        msg.Length.Should().Be(ByteArray3.Length);
                        msg.ToArray().Should().BeEquivalentTo(ByteArray3);
                        return true;
                    },
                    default);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                s.DequeueZeroCopy(
                    (msg, _) =>
                    {
                        msg.Length.Should().Be(ByteArray3.Length);
                        msg.ToArray().Should().BeEquivalentTo(ByteArray3);
                        return true;
                    },
                    default);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                s.DequeueZeroCopy(
                    (msg, _) =>
                    {
                        msg.Length.Should().Be(ByteArray2.Length);
                        msg.ToArray().Should().BeEquivalentTo(ByteArray2);
                        return true;
                    },
                    default);

                p.TryEnqueue(ByteArray2).Should().BeTrue();
                s.DequeueZeroCopy(
                    (msg, _) =>
                    {
                        msg.Length.Should().Be(ByteArray2.Length);
                        msg.ToArray().Should().BeEquivalentTo(ByteArray2);
                        return true;
                    },
                    default);
            });

        [Fact]
        public void CanEnqueueDequeueWrappedMessage()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(128);
                using var s = CreateSubscriber(128);

                p.TryEnqueue(ByteArray50).Should().BeTrue();
                var message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray50);

                p.TryEnqueue(ByteArray50).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray50);

                p.TryEnqueue(ByteArray50).Should().BeTrue();
                message = s.Dequeue(default);
                message.ToArray().Should().BeEquivalentTo(ByteArray50);
            });

        [Fact]
        public void CannotEnqueuePastCapacity()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(40);

                p.TryEnqueue(ByteArray3).Should().BeTrue();
                p.TryEnqueue(ByteArray1).Should().BeFalse();
            });

        [Fact]
        public void CannotEnqueuePastCapacityZeroCopy()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(40);

                p.TryEnqueueZeroCopy(
                    ByteArray3.Length,
                    (buffer, _) =>
                    {
                        buffer.Write(ByteArray3);
                        return ByteArray3.Length;
                    },
                    default).Should().BeTrue();

                p.TryEnqueueZeroCopy(
                    ByteArray1.Length,
                    (_, _) =>
                    {
                        Assert.True(false, "Shouldn't invoke write function if unable to reserve.");
                        return -1;
                    },
                    default).Should().BeFalse();
            });

        [Fact]
        public void DisposeShouldNotThrow()
            => BeforeAfterTest(() =>
            {
                var p = CreatePublisher(40);
                p.TryEnqueue(ByteArray3).Should().BeTrue();

                using var s = CreateSubscriber(40);
                p.Dispose();

                s.Dequeue(default);
            });

        [Fact]
        public void CannotReadAfterProducerIsDisposed()
            => BeforeAfterTest(() =>
            {
                var p = CreatePublisher(40);
                p.TryEnqueue(ByteArray3).Should().BeTrue();
                using (var s = CreateSubscriber(40))
                    p.Dispose();

                using (CreatePublisher(40))
                using (var s = CreateSubscriber(40))
                {
                    s.TryDequeue(default, out var message).Should().BeFalse();
                }
            });

        [Theory]
        [Repeat(10)]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "The extra argument is needed by the Repeat attribute.")]
        [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "The extra argument is needed by the Repeat attribute.")]
        public async Task CanDisposeQueueAsync(int i)
            => await BeforeAfterTestAsync(async () =>
            {
                using (var s = CreateSubscriber(1024))
                {
                    _ = Task.Run(() => s.Dequeue(default));
                    await Task.Delay(100);
                }
            });

        [Fact]
        public void CanCircleBuffer()
            => BeforeAfterTest(() =>
            {
                using var p = CreatePublisher(1024);
                using var s = CreateSubscriber(1024);

                var message = Enumerable.Range(100, 66).Select(i => (byte)i).ToArray();

                for (var i = 0; i < 20000; i++)
                {
                    p.TryEnqueue(message).Should().BeTrue();
                    var result = s.Dequeue(default);
                    result.Span.SequenceEqual(message).Should().BeTrue();
                }
            });

        private IChannel CreateChannel(long capacity)
            => queueFactory.CreateChannel(
                new QueueOptions(queueName, fixture.Path, capacity));
        private IChannel ConsumeChannel(long capacity)
            => queueFactory.CreateChannel(
                new QueueOptions(queueName, fixture.Path, capacity), true);

        private IPublisher CreatePublisher(long capacity)
            => queueFactory.CreatePublisher(
                new QueueOptions(queueName, fixture.Path, capacity));

        private ISubscriber CreateSubscriber(long capacity)
            => queueFactory.CreateSubscriber(
                new QueueOptions(queueName, fixture.Path, capacity));
    }
}
