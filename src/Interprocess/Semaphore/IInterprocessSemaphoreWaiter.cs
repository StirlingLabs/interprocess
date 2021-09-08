using System;

namespace Cloudtoid.Interprocess
{
    public interface IInterprocessSemaphoreWaiter
    {
        bool Wait(int millisecondsTimeout);
    }
}
