using System;
using System.Diagnostics;
#if DEBUG
using Xunit.Abstractions;
#endif

namespace Cloudtoid.Interprocess
{
    public static class DebugContext
    {
#if DEBUG
        [ThreadStatic]
        private static ITestOutputHelper? testOutputHelper;
        public static ITestOutputHelper? TestOutputHelper
        {
            get => testOutputHelper;
            set => testOutputHelper = value;
        }
#else
        public static object? TestOutputHelper
        {
            get => null;
            // ReSharper disable once ValueParameterNotUsed
            set { }
        }
#endif
        [Conditional("DEBUG")]
        public static void WriteLine(string line)
#if DEBUG
            => TestOutputHelper?.WriteLine(line);
#else
            { }
#endif
    }
}
