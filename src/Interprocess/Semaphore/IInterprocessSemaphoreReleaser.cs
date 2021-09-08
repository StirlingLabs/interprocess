using System;

namespace Cloudtoid.Interprocess
{
    public interface IInterprocessSemaphoreReleaser
    {
        void Release();
    }
}
