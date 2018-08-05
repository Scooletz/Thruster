using BenchmarkDotNet.Running;

namespace Thruster.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<AgainstSharedPool>();
        }
    }
}
