using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Cloudtoid.Interprocess.Benchmark
{
    public sealed class Program
    {
        public static void Main()
            => BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
