using System;

namespace Cloudtoid.Interprocess
{
    public interface IInterprocessSemaphoreWaiter : IDisposable
    {
        bool Wait(int millisecondsTimeout);
    }
}
