using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cloudtoid.Interprocess
{
    public sealed class QueueFactory : IQueueFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public QueueFactory()
            : this(NullLoggerFactory.Instance)
        {
        }

        public QueueFactory(ILoggerFactory loggerFactory)
        {
            Util.Ensure64Bit();
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Creates a queue message publisher.
        /// </summary>
        public IPublisher CreatePublisher(QueueOptions options)
            => new Publisher(options ?? throw new ArgumentNullException(nameof(options)), loggerFactory);

        /// <summary>
        /// Creates a queue message subscriber.
        /// </summary>
        public ISubscriber CreateSubscriber(QueueOptions options)
            => new Subscriber(options ?? throw new ArgumentNullException(nameof(options)), loggerFactory);

        public IChannel CreateChannel(QueueOptions options, bool asClient = false)
            => new Channel(options ?? throw new ArgumentNullException(nameof(options)), this, asClient);
    }
}
