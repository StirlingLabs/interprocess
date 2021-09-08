using System;
using Microsoft.Extensions.Logging;

namespace Cloudtoid.Interprocess
{
    internal sealed class Channel : IChannel
    {
        private readonly Publisher publisher;

        private readonly Subscriber subscriber;

        public Channel(QueueOptions options, IQueueFactory queueFactory, bool asClient = false)
        {
            if (asClient)
            {
                var pubOpts = new QueueOptions("S" + options.QueueName, options.Path, options.BytesCapacity);
                publisher = (Publisher)queueFactory.CreatePublisher(pubOpts);

                var subOpts = new QueueOptions("P" + options.QueueName, options.Path, options.BytesCapacity);
                subscriber = (Subscriber)queueFactory.CreateSubscriber(subOpts);
            }
            else
            {
                var pubOpts = new QueueOptions("P" + options.QueueName, options.Path, options.BytesCapacity);
                publisher = (Publisher)queueFactory.CreatePublisher(pubOpts);

                var subOpts = new QueueOptions("S" + options.QueueName, options.Path, options.BytesCapacity);
                subscriber = (Subscriber)queueFactory.CreateSubscriber(subOpts);
            }
        }

        public Channel(string queueName, long bytesCapacity, IQueueFactory queueFactory, bool asClient = false)
        {
            if (asClient)
            {
                var pubOpts = new QueueOptions("S" + queueName, bytesCapacity);
                publisher = (Publisher)queueFactory.CreatePublisher(pubOpts);

                var subOpts = new QueueOptions("P" + queueName, bytesCapacity);
                subscriber = (Subscriber)queueFactory.CreateSubscriber(subOpts);
            }
            else
            {
                var pubOpts = new QueueOptions("P" + queueName, bytesCapacity);
                publisher = (Publisher)queueFactory.CreatePublisher(pubOpts);

                var subOpts = new QueueOptions("S" + queueName, bytesCapacity);
                subscriber = (Subscriber)queueFactory.CreateSubscriber(subOpts);
            }
        }

        public Channel(string queueName, string path, long bytesCapacity, IQueueFactory queueFactory, bool asClient = false)
        {
            if (asClient)
            {
                var pubOpts = new QueueOptions("S" + queueName, path, bytesCapacity);
                publisher = (Publisher)queueFactory.CreatePublisher(pubOpts);

                var subOpts = new QueueOptions("P" + queueName, path, bytesCapacity);
                subscriber = (Subscriber)queueFactory.CreateSubscriber(subOpts);
            }
            else
            {
                var pubOpts = new QueueOptions("P" + queueName, path, bytesCapacity);
                publisher = (Publisher)queueFactory.CreatePublisher(pubOpts);

                var subOpts = new QueueOptions("S" + queueName, path, bytesCapacity);
                subscriber = (Subscriber)queueFactory.CreateSubscriber(subOpts);
            }
        }

        public IPublisher Publisher => publisher;

        public ISubscriber Subscriber => subscriber;
        public void Dispose()
        {
            publisher.Dispose();
            subscriber.Dispose();
        }
    }
}
