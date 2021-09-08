using System.Runtime.InteropServices;
using Cloudtoid.Interprocess.Semaphore.Linux;
using Cloudtoid.Interprocess.Semaphore.MacOS;
using Cloudtoid.Interprocess.Semaphore.Windows;

namespace Cloudtoid.Interprocess
{
    /// <summary>
    /// This class opens or creates platform agnostic named semaphore. Named
    /// semaphores are synchronization constructs accessible across processes.
    /// </summary>
    public static class InterprocessSemaphore
    {
        public static IInterprocessSemaphore Create(string name)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new SemaphoreWindows(name);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return new SemaphoreMacOS(name);

            return new SemaphoreLinux(name);
        }

        public static IInterprocessSemaphoreWaiter CreateWaiter(string name)
            => Create(name);

        public static IInterprocessSemaphoreReleaser CreateReleaser(string name)
            => Create(name);
    }
}
