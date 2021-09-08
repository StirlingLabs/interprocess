using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Cloudtoid.Interprocess.Posix;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Cloudtoid.Interprocess.Tests
{
    [Collection("SharedNamespaceTests")]
    public class SemaphoreTests : LoggingTestBase
    {
        public SemaphoreTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
            => DebugContext.TestOutputHelper = testOutputHelper;

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        [Fact(Platforms = Platform.Windows)]
        public void CanReleaseAndWaitWindows()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using var sem = new Windows.Semaphore(name);
                sem.Wait(10).Should().BeFalse();
                sem.Release();
                sem.Release();
                sem.Wait(-1).Should().BeTrue();
                sem.Wait(10).Should().BeTrue();
                sem.Wait(0).Should().BeFalse();
                sem.Wait(10).Should().BeFalse();
                sem.Release();
                sem.Wait(10).Should().BeTrue();
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
#endif
        [Fact(Platforms = Platform.Linux | Platform.FreeBSD)]
        public void CanReleaseAndWaitLinux()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using var sem = new Linux.Semaphore(name, deleteOnDispose: true);
                sem.Wait(10).Should().BeFalse();
                sem.Release();
                sem.Release();
                sem.Wait(-1).Should().BeTrue();
                sem.Wait(10).Should().BeTrue();
                sem.Wait(0).Should().BeFalse();
                sem.Wait(10).Should().BeFalse();
                sem.Release();
                sem.Wait(10).Should().BeTrue();
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("macos")]
#endif
        [Fact(Platforms = Platform.OSX)]
        public void CanReleaseAndWaitMacOS()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using var sem = new MacOS.Semaphore(name, deleteOnDispose: true);
                sem.Wait(10).Should().BeFalse();
                sem.Release();
                sem.Release();
                sem.Wait(-1).Should().BeTrue();
                sem.Wait(10).Should().BeTrue();
                sem.Wait(0).Should().BeFalse();
                sem.Wait(10).Should().BeFalse();
                sem.Release();
                sem.Wait(10).Should().BeTrue();
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        [Fact(Platforms = Platform.Windows)]
        public void CanCreateMultipleSemaphoresWithSameNameWindows()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using var sem1 = new Windows.Semaphore(name);
                using var sem2 = new Windows.Semaphore(name);
                sem2.Release();
                sem1.Wait(10).Should().BeTrue();
                sem1.Wait(10).Should().BeFalse();
                sem2.Wait(10).Should().BeFalse();
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
#endif
        [Fact(Platforms = Platform.Linux | Platform.FreeBSD)]
        public void CanCreateMultipleSemaphoresWithSameNameLinux()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using var sem1 = new Linux.Semaphore(name, deleteOnDispose: true);
                using var sem2 = new Linux.Semaphore(name, deleteOnDispose: false);
                sem2.Release();
                sem1.Wait(10).Should().BeTrue();
                sem1.Wait(10).Should().BeFalse();
                sem2.Wait(10).Should().BeFalse();
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("macos")]
#endif
        [Fact(Platforms = Platform.OSX)]
        public void CanCreateMultipleSemaphoresWithSameNameMacOS()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using var sem1 = new MacOS.Semaphore(name, deleteOnDispose: true);
                using var sem2 = new MacOS.Semaphore(name, deleteOnDispose: false);
                sem2.Release();
                sem1.Wait(10).Should().BeTrue();
                sem1.Wait(10).Should().BeFalse();
                sem2.Wait(10).Should().BeFalse();
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        [Fact(Platforms = Platform.Windows)]
        public void CanReuseSameSemaphoreNameWindows()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                using (var sem = new Windows.Semaphore(name))
                {
                    sem.Wait(10).Should().BeFalse();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                using (var sem2 = new Windows.Semaphore(name))
                {
                    using (var sem = new Windows.Semaphore(name))
                    {
                        sem.Wait(10).Should().BeFalse();
                        sem.Release();
                        sem.Wait(-1).Should().BeTrue();
                        sem.Release();
                    }

                    sem2.Wait(10).Should().BeTrue();
                    sem2.Release();
                    sem2.Wait(-1).Should().BeTrue();
                    sem2.Release();
                }
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("freebsd")]
#endif
        [Fact(Platforms = Platform.Linux | Platform.FreeBSD)]
        public void CanReuseSameSemaphoreNameLinux()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                Output.WriteLine($"Name: {name}");

                Assert.Throws<PosixSempahoreNotExistsException>(() => Linux.Interop.Unlink("/" + name));

                using (var sem = new Linux.Semaphore(name, deleteOnDispose: true))
                {
                    Output.WriteLine($"Handle 1: {Unsafe.As<Linux.Semaphore, nint>(ref Unsafe.AsRef(sem)):X}");
                    sem.Wait(10).Should().BeFalse();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                Assert.Throws<PosixSempahoreNotExistsException>(() => Linux.Interop.Unlink("/" + name));

                using (var sem = new Linux.Semaphore(name, deleteOnDispose: false))
                {
                    Output.WriteLine($"Handle 2: {Unsafe.As<Linux.Semaphore, nint>(ref Unsafe.AsRef(sem)):X}");
                    sem.Wait(10).Should().BeFalse();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                using (var sem = new Linux.Semaphore(name, deleteOnDispose: true))
                {
                    Output.WriteLine($"Handle 3: {Unsafe.As<Linux.Semaphore, nint>(ref Unsafe.AsRef(sem)):X}");
                    sem.Wait(10).Should().BeTrue();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                Assert.Throws<PosixSempahoreNotExistsException>(() => Linux.Interop.Unlink("/" + name));
            });

#if NET5_0_OR_GREATER
        [SupportedOSPlatform("macos")]
#endif
        [Fact(Platforms = Platform.OSX)]

        public void CanReuseSameSemaphoreNameMacOS()
            => BeforeAfterTest(() =>
            {
                var name = InterprocessSemaphore.GenerateName();
                Output.WriteLine($"Name: {name}");

                Assert.Throws<PosixSempahoreNotExistsException>(() => MacOS.Interop.Unlink("/" + name));

                using (var sem = new MacOS.Semaphore(name, deleteOnDispose: true))
                {
                    Output.WriteLine($"Handle 1: {Unsafe.As<MacOS.Semaphore, nint>(ref Unsafe.AsRef(sem)):X}");
                    sem.Wait(10).Should().BeFalse();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                Assert.Throws<PosixSempahoreNotExistsException>(() => MacOS.Interop.Unlink("/" + name));

                using (var sem = new MacOS.Semaphore(name, deleteOnDispose: false))
                {
                    Output.WriteLine($"Handle 2: {Unsafe.As<MacOS.Semaphore, nint>(ref Unsafe.AsRef(sem)):X}");
                    sem.Wait(10).Should().BeFalse();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                using (var sem = new MacOS.Semaphore(name, deleteOnDispose: true))
                {
                    Output.WriteLine($"Handle 3: {Unsafe.As<MacOS.Semaphore, nint>(ref Unsafe.AsRef(sem)):X}");
                    sem.Wait(10).Should().BeTrue();
                    sem.Release();
                    sem.Wait(-1).Should().BeTrue();
                    sem.Release();
                }

                Assert.Throws<PosixSempahoreNotExistsException>(() => MacOS.Interop.Unlink("/" + name));
            });
    }
}