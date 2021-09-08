using System;

namespace Cloudtoid.Interprocess
{
    public interface IInterprocessSemaphore : IInterprocessSemaphoreReleaser, IInterprocessSemaphoreWaiter, IDisposable
    {
    }
}
