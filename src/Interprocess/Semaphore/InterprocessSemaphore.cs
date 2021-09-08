using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Cloudtoid.Interprocess
{
    /// <summary>
    /// This class opens or creates platform agnostic named semaphore. Named
    /// semaphores are synchronization constructs accessible across processes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("Interoperability", "CA1416", Justification = "Interop with all implemented platforms")]
    public readonly struct InterprocessSemaphore : IInterprocessSemaphore, IInterprocessSemaphoreDestroyer
    {
        private static readonly PlatformId Platform;

#pragma warning disable 169 // Structure is cast to platform-specific structure
        private readonly IntPtr handle;
#pragma warning restore 169

        static InterprocessSemaphore()
            => Platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? PlatformId.Windows
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? PlatformId.MacOs
                    : PlatformId.Linux;

        private enum PlatformId
        {
            Windows,
            MacOs,
            Linux
        }

        public static InterprocessSemaphore Create(out string name, int length = 29)
        {
            name = GenerateName(length);
            return Create(name);
        }

        public static string GenerateName(int length = 22)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && length >= 30)
                throw new ArgumentOutOfRangeException(nameof(length), "MacOS semaphores must have names of less than 30 characters.");

#if NETSTANDARD2_0
            var s = new char[length];
            var v = DateTime.Now.Ticks;
            var rng = RandomNumberGenerator.Create();
            var intBuf = new byte[4];

            int GetRandomInt32(int minIncl, int maxExcl)
            {
                rng!.GetBytes(intBuf);
                return minIncl + MemoryMarshal.Read<int>(intBuf) % (maxExcl - minIncl);
            }

            if (length > 0)
                s[0] = (char)('a' + GetRandomInt32(0, 26));

            var c = 1;
            for (; c < length && v != 0; ++c)
            {
                s[c] = (char)('a' + (v % 26));
                v /= 26;
            }

            for (; c < length; ++c)
                s[c] = (char)('a' + GetRandomInt32(0, 26));
            return new string(s);
#else
            return string.Create(length, length, (s, l) =>
            {
                var v = DateTime.Now.Ticks;
                if (l > 0)
                    s[0] = (char)('a' + RandomNumberGenerator.GetInt32(0, 26));

                var c = 1;
                for (; c < l && v != 0; ++c)
                {
                    s[c] = (char)('a' + (v % 26));
                    v /= 26;
                }

                for (; c < l; ++c)
                    s[c] = (char)('a' + RandomNumberGenerator.GetInt32(0, 26));
            });
#endif
        }

        public static InterprocessSemaphore Create(ref string? name, out bool createdName, int length = 30)
        {
            if (name is null)
            {
                createdName = true;
                return Create(out name);
            }

            createdName = false;
            return Create(name);
        }

        public static InterprocessSemaphore Create(string name)
        {
#if NETSTANDARD2_0
            if (name.IndexOf(Path.PathSeparator) != -1)
#else
            if (name.Contains(Path.PathSeparator, StringComparison.Ordinal))
#endif
                throw new ArgumentException("Name must not contain path separators.", nameof(name));

            switch (Platform)
            {
                case PlatformId.Windows:
                    return Unsafe.As<Windows.Semaphore, InterprocessSemaphore>(ref Unsafe.AsRef(new Windows.Semaphore(name)));
                case PlatformId.MacOs:
                    if (name.Length >= 30)
                        throw new ArgumentOutOfRangeException(nameof(name), $"MacOS semaphores must have names of less than 30 characters. \"{name}\" is {name.Length} characters.");

                    return Unsafe.As<MacOS.Semaphore, InterprocessSemaphore>(ref Unsafe.AsRef(new MacOS.Semaphore(name)));
                case PlatformId.Linux:
                    return Unsafe.As<Linux.Semaphore, InterprocessSemaphore>(ref Unsafe.AsRef(new Linux.Semaphore(name)));
                default: throw new PlatformNotSupportedException();
            }
        }

        public void Release()
        {
            switch (Platform)
            {
                case PlatformId.Windows:
                    Unsafe.As<InterprocessSemaphore, Windows.Semaphore>(ref Unsafe.AsRef(this)).Release();
                    break;
                case PlatformId.MacOs:
                    Unsafe.As<InterprocessSemaphore, MacOS.Semaphore>(ref Unsafe.AsRef(this)).Release();
                    break;
                case PlatformId.Linux:
                    Unsafe.As<InterprocessSemaphore, Linux.Semaphore>(ref Unsafe.AsRef(this)).Release();
                    break;
                default: throw new PlatformNotSupportedException();
            }
        }

        public bool Wait(int millisecondsTimeout)
            => Platform switch
            {
                PlatformId.Windows => Unsafe.As<InterprocessSemaphore, Windows.Semaphore>(ref Unsafe.AsRef(this))
                    .Wait(millisecondsTimeout),
                PlatformId.MacOs => Unsafe.As<InterprocessSemaphore, MacOS.Semaphore>(ref Unsafe.AsRef(this))
                    .Wait(millisecondsTimeout),
                PlatformId.Linux => Unsafe.As<InterprocessSemaphore, Linux.Semaphore>(ref Unsafe.AsRef(this))
                    .Wait(millisecondsTimeout),
                _ => throw new PlatformNotSupportedException()
            };

        public void Dispose()
        {
            switch (Platform)
            {
                case PlatformId.Windows:
                    Unsafe.As<InterprocessSemaphore, Windows.Semaphore>(ref Unsafe.AsRef(this)).Dispose();
                    break;
                case PlatformId.MacOs:
                    Unsafe.As<InterprocessSemaphore, MacOS.Semaphore>(ref Unsafe.AsRef(this)).Dispose();
                    break;
                case PlatformId.Linux:
                    Unsafe.As<InterprocessSemaphore, Linux.Semaphore>(ref Unsafe.AsRef(this)).Dispose();
                    break;
                default: return;
            }
        }

        void IInterprocessSemaphoreDestroyer.Destroy()
        {
            switch (Platform)
            {
                case PlatformId.Windows:
                    throw new PlatformNotSupportedException();
                case PlatformId.MacOs:
                    Unsafe.As<InterprocessSemaphore, MacOS.Semaphore>(ref Unsafe.AsRef(this)).Destroy();
                    break;
                case PlatformId.Linux:
                    Unsafe.As<InterprocessSemaphore, Linux.Semaphore>(ref Unsafe.AsRef(this)).Destroy();
                    break;
                default: return;
            }
        }
    }
}