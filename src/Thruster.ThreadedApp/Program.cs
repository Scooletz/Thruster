using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;

namespace Thruster.ThreadedApp
{
    class Program
    {
        static readonly int[] Creators = Enumerable.Range(1, Environment.ProcessorCount).ToArray();

        static void Main(string[] args)
        {
            CompareWithShared();
            //var spans = Enumerable.Range(1, 21).Select(i => RunPipe()).ToArray();
            //Array.Sort(spans);

            //Console.WriteLine($"Pipelining Thruster (mean time) took: {spans[spans.Length / 2]}");
        }

        static void CompareWithShared()
        {
            var shared = Run(MemoryPool<byte>.Shared).GetAwaiter().GetResult();
            Console.WriteLine($"Running Shared took:   {shared}");

            using (var pool = new FastMemoryPool<byte>())
            {
                var thruster = Run(pool).GetAwaiter().GetResult();
                Console.WriteLine($"Running Thruster took: {thruster}");
            }
        }

        static TimeSpan RunPipe()
        {
            using (var pool = new FastMemoryPool<byte>())
            {
                var sw = Stopwatch.StartNew();

                var pipe = new Pipe(new PipeOptions(pool));

                const int writeLenght = 57;
                const long innerLoopCount = 64 * 1024 * 1204;

                var writing = Task.Run(async () =>
                {
                    for (int i = 0; i < innerLoopCount; i++)
                    {
                        pipe.Writer.GetMemory(writeLenght);
                        pipe.Writer.Advance(writeLenght);
                        await pipe.Writer.FlushAsync();
                    }
                });

                var reading = Task.Run(async () =>
                {
                    long remaining = innerLoopCount * writeLenght;
                    while (remaining != 0)
                    {
                        var result = await pipe.Reader.ReadAsync();
                        remaining -= result.Buffer.Length;
                        pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
                    }
                });

                Task.WaitAll(writing, reading);

                return sw.Elapsed;
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
            for (var i = 0; i < 100000000; i++)
            {
                using (var o1 = pool.Rent(1))
                {
                    o1.Memory.Span[1] = 1;
                }
            }
        }
    }
}
