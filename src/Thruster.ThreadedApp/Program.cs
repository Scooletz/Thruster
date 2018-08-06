using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Thruster.ThreadedApp
{
    class Program
    {
        static readonly int[] Creators = Enumerable.Range(1, Environment.ProcessorCount).ToArray();

        static void Main(string[] args)
        {
            var shared = Run(MemoryPool<byte>.Shared).GetAwaiter().GetResult();

            using (var pool = new FastMemoryPool<byte>())
            {
                var thruster = Run(pool).GetAwaiter().GetResult();

                Console.WriteLine($"Running Shared took:   {shared}");
                Console.WriteLine($"Running Thruster took: {thruster}");
            }
        }

        static async Task<TimeSpan> Run(MemoryPool<byte> pool)
        {
            var sw = Stopwatch.StartNew();
            await Task.WhenAll(Creators.Select((i) => Task.Run(() => RunSingle(pool)))).ConfigureAwait(false);
            return sw.Elapsed;
        }

        static void RunSingle(MemoryPool<byte> pool)
        {
            for (var i = 0; i < 10000000; i++)
            {
                using (var o1 = pool.Rent(1))
                using (var o2 = pool.Rent(1))
                {
                    o1.Memory.Span[1] = 1;
                    o2.Memory.Span[1] = 1;
                }
            }
        }
    }
}
