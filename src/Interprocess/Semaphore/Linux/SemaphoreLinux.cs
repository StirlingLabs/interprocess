using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Cloudtoid.Interprocess.Semaphore.Linux
{
    internal class SemaphoreLinux : IInterprocessSemaphoreWaiter, IInterprocessSemaphoreReleaser
    {
        private const string HandleNamePrefix = "/ct.ip.";
        private readonly string name;
        private readonly bool deleteOnDispose;
        private readonly IntPtr handle;

        internal SemaphoreLinux(string name, bool deleteOnDispose = false)
        {
            this.name = name = HandleNamePrefix + name;
            this.deleteOnDispose = deleteOnDispose;
            handle = Interop.CreateOrOpenSemaphore(name, 0);
        }

        ~SemaphoreLinux()
            => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Interop.Close(handle);

            if (deleteOnDispose)
                Interop.Unlink(name);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
            => Interop.Release(handle);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout)
            => Interop.Wait(handle, millisecondsTimeout);
    }
}