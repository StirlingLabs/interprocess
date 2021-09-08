using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

using SysSemaphore = System.Threading.Semaphore;

namespace Cloudtoid.Interprocess.Semaphore.Windows
{
    // just a wrapper over the Windows named semaphore
    internal sealed class SemaphoreWindows : IInterprocessSemaphore
    {
        private const string HandleNamePrefix = @"Global\CT.IP.";
        private readonly SysSemaphore handle;

        [SuppressMessage("Interoperability", "CA1416", Justification = "Used only on Windows platforms")]
        internal SemaphoreWindows(string name)
        {
            //2021-06-02 Fabian Ramirez - Adding code to handle permissions when accessing the semaphore from different windows accounts
            string prefixedName = HandleNamePrefix + name;

            handle = new SysSemaphore(0, int.MaxValue, prefixedName);

            try
            {
                SemaphoreSecurity semaphoreSecurity = new SemaphoreSecurity();
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                semaphoreSecurity.AddAccessRule(new SemaphoreAccessRule(sid, SemaphoreRights.FullControl, AccessControlType.Allow));
                handle.SetAccessControl(semaphoreSecurity);
            }
            catch
            {
                // failed to allow other processes to access the semaphore
            }

            //2021-06-02 Fabian Ramirez - END
        }

        public void Dispose()
            => handle.Dispose();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
            => handle.Release();

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout)
            => handle.WaitOne(millisecondsTimeout);
    }
}