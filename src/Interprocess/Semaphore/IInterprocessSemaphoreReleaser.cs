using System;

namespace Cloudtoid.Interprocess
{
    public interface IInterprocessSemaphoreReleaser : IDisposable
    {
        void Release();
    }
}
