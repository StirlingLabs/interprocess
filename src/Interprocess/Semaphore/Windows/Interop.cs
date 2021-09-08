using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Cloudtoid.Interprocess.Windows
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Matching the exact names in Windows")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "Matching the exact names in Windows")]
    internal static class Interop
    {
        private const string Lib = "kernel32";

        internal const uint MAXIMUM_ALLOWED = 0x02000000;
        internal const uint SYNCHRONIZE = 0x00100000;
        internal const uint SEMAPHORE_MODIFY_STATE = 0x00000002;

        internal const uint ERROR_ALREADY_EXISTS = 0xB7;
        private const int WAIT_TIMEOUT = 0x102;
        private const int WAIT_OBJECT_0 = 0;

#if WINDOWS_GLOBAL_SEMAPHORES
        private static readonly SecurityIdentifier WorldSid = new(WellKnownSidType.WorldSid, null);

        private static readonly SemaphoreSecurity WorldAllAccess;
        static Interop()
        {
            WorldAllAccess = new();
            var sid = WorldSid;
            WorldAllAccess.AddAccessRule(new(sid, SemaphoreRights.FullControl, AccessControlType.Allow));
        }
#endif

#if NET5_0_OR_GREATER
        [SuppressGCTransition]
#endif
        [DllImport(Lib, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

#if WINDOWS_GLOBAL_SEMAPHORES
#if NET5_0_OR_GREATER
        [SuppressGCTransition]
#endif
        [DllImport(Lib, EntryPoint = "OpenSemaphoreW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr OpenSemaphore(uint desiredAccess, bool inheritHandle, string name);
#endif

#if NET5_0_OR_GREATER
        [SuppressGCTransition]
#endif
        [DllImport(Lib, EntryPoint = "CreateSemaphoreExW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr CreateSemaphoreEx(IntPtr lpSecurityAttributes, int initialCount, int maximumCount, string? name, uint flags, uint desiredAccess);

#if NET5_0_OR_GREATER
        [SuppressGCTransition]
#endif
        [DllImport(Lib, SetLastError = true)]
        internal static extern bool ReleaseSemaphore(IntPtr handle, int releaseCount, out int previousCount);

        [DllImport(Lib, ExactSpelling = true, SetLastError = true)]
        internal static extern int WaitForSingleObject(IntPtr handle, int timeout);

        internal static IntPtr CreateOrOpenSemaphore(string name, int initialCount)
        {
            const uint accessRights = MAXIMUM_ALLOWED | SYNCHRONIZE | SEMAPHORE_MODIFY_STATE;
            var handle = CreateSemaphoreEx(IntPtr.Zero, initialCount, int.MaxValue, name, 0, accessRights);

            var createdNew = Marshal.GetLastWin32Error() != ERROR_ALREADY_EXISTS;

            if (handle <= (nint)0)
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new NotSupportedException();

#if WINDOWS_GLOBAL_SEMAPHORES
            if (!createdNew)
                return handle;

            try
            {
                using var semaphore = System.Threading.Semaphore.OpenExisting(name);
                semaphore.SetAccessControl(WorldAllAccess);
            }
            catch
            {
                // failed to allow other processes to access the semaphore, still ok
            }
#endif

            return handle;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Release(IntPtr handle)
            => Release(handle, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Release(IntPtr handle, int count)
        {
            if (!ReleaseSemaphore(handle, count, out _))
                throw new SemaphoreFullException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Wait(IntPtr handle, int millisecondsTimeout)
        {
            var result = WaitForSingleObject(handle, millisecondsTimeout);
            return result switch
            {
                WAIT_OBJECT_0 => true,
                WAIT_TIMEOUT => false,
                _ => throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()) ?? new NotSupportedException(),
            };
        }
    }
}
