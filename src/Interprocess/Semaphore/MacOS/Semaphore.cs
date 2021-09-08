using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Cloudtoid.Interprocess.MacOS
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("macos")]
#endif
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Semaphore : IInterprocessSemaphore, IInterprocessSemaphoreDestroyer
    {
        private const string HandleNamePrefix = "/";

        private static readonly ConcurrentDictionary<IntPtr, string> Names = new();
        private static readonly HashSet<IntPtr> DestroyOnExit = new();

        private readonly IntPtr handle;

        private static readonly int ShiftBits = (IntPtr.Size * 8) - 1;

        private static readonly nint BitMask = (nint)1 << ShiftBits;

        private static readonly nint InverseBitMask = ~BitMask;

        static Semaphore()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                lock (DestroyOnExit)
                {
                    foreach (var handle in DestroyOnExit)
                        if (Names.TryGetValue(handle, out var name))
                            Interop.UnlinkNoThrow(name);
                }
            };
        }

        internal Semaphore(string name, bool deleteOnDispose = false)
        {
            var prefixedName = HandleNamePrefix + name;
            handle = Interop.CreateOrOpenSemaphore(prefixedName, 0) | (deleteOnDispose ? BitMask : 0);
            Names[Handle] = name;
            if (!deleteOnDispose) return;
            lock (DestroyOnExit) DestroyOnExit.Add(Handle);
        }

        private bool DestroyOnDispose => (handle & BitMask) != 0;
        private IntPtr Handle => handle & InverseBitMask;
        private string Name => HandleNamePrefix + Names[Handle];

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Interop.Close(Handle);
            if (DestroyOnDispose)
                Destroy();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Destroy()
        {
            Interop.Unlink(Name);
            lock (DestroyOnExit)
                DestroyOnExit.Remove(Handle);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
            => Interop.Release(Handle);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int millisecondsTimeout)
            => Interop.Wait(Handle, millisecondsTimeout);
    }
}
