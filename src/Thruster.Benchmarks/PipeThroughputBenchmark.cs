using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Thruster.Benchmarks
{
    public class PipeThroughputBenchmark : IDisposable
    {
        const int WriteLenght = 57;
        const int Count = 512;

        readonly MemoryPool<byte> thruster;
        readonly Pipe thrusterPipe;

        readonly MemoryPool<byte> kestrel;
        readonly Pipe kestrelPipe;

        readonly Pipe sharedPipe;

        public PipeThroughputBenchmark()
        {
            kestrel = KestrelMemoryPool.Create();
            kestrelPipe = new Pipe(new PipeOptions(kestrel));

            thruster = new FastMemoryPool<byte>();
            thrusterPipe = new Pipe(new PipeOptions(thruster));

            sharedPipe = new Pipe(new PipeOptions(MemoryPool<byte>.Shared));
        }

        [Benchmark(OperationsPerInvoke = Count)]
        public void ParseLiveAspNetTwoTasks_Thruster() => ParseLiveTwoTasksImpl(thrusterPipe);

        [Benchmark(OperationsPerInvoke = Count)]
        public void ParseLiveAspNetTwoTasks_Shared() => ParseLiveTwoTasksImpl(sharedPipe);

        [Benchmark(OperationsPerInvoke = Count)]
        public void ParseLiveAspNetTwoTasks_Kestrel() => ParseLiveTwoTasksImpl(kestrelPipe);

        static void ParseLiveTwoTasksImpl(Pipe pipe)
        {
            var writing = Task.Run(async () =>
            {
                for (var i = 0; i < Count; i++)
                {
                    pipe.Writer.GetMemory(WriteLenght);
                    pipe.Writer.Advance(WriteLenght);
                    await pipe.Writer.FlushAsync();
                }
            });

            var reading = Task.Run(async () =>
            {
                long remaining = Count * WriteLenght;
                while (remaining != 0)
                {
                    var result = await pipe.Reader.ReadAsync();
                    remaining -= result.Buffer.Length;
                    pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
                }
            });

            Task.WaitAll(writing, reading);
        }

        [Benchmark(OperationsPerInvoke = Count)]
        public void ParseLiveAspNetInline_Thruster() => ParseImpl(thrusterPipe);

        [Benchmark(OperationsPerInvoke = Count)]
        public void ParseLiveAspNetInline_Shared() => ParseImpl(sharedPipe);

        [Benchmark(OperationsPerInvoke = Count)]
        public void ParseLiveAspNetInline_Kestrel() => ParseImpl(kestrelPipe);

        static void ParseImpl(Pipe pipe)
        {
            for (var i = 0; i < Count; i++)
            {
                pipe.Writer.GetMemory(WriteLenght);
                pipe.Writer.Advance(WriteLenght);
                pipe.Writer.FlushAsync().GetAwaiter().GetResult();
                var result = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
                pipe.Reader.AdvanceTo(result.Buffer.End, result.Buffer.End);
            }
        }

        public void Dispose()
        {
            kestrel.Dispose();
            thruster.Dispose();
        }
    }
}