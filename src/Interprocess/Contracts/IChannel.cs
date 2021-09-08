using System;

namespace Cloudtoid.Interprocess
{
    public interface IChannel : IDisposable
    {
        public IPublisher Publisher { get; }
        public ISubscriber Subscriber { get; }
    }
}
