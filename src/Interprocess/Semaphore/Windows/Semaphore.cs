using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Cloudtoid.Interprocess.Windows
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Semaphore : IInterprocessSemaphore
    {
        private readonly IntPtr handle;

        internal Semaphore(string name)
            => handle = Interop.CreateOrOpenSemaphore(name + "_Semaphore", 0);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
            => Interop.CloseHandle(handle);

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
