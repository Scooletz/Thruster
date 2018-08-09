using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Thruster.Benchmarks
{
    public class NestedRentingBenchmark : IDisposable
    {
        readonly MemoryPool<byte> thruster;
        readonly MemoryPool<byte> shared;
        readonly MemoryPool<byte> kestrel;

        public NestedRentingBenchmark()
        {
            thruster = new FastMemoryPool<byte>();
            shared = MemoryPool<byte>.Shared;
            kestrel = KestrelMemoryPool.Create();
        }

        [Benchmark] public void Rent_1_Thruster() => Kilo1(thruster);
        [Benchmark] public void Rent_1_Shared() => Kilo1(shared);
        [Benchmark] public void Rent_1_Kestrel() => Kilo1(kestrel);

        [Benchmark] public void Rent_2_Thruster() => Kilo2(thruster);
        [Benchmark] public void Rent_2_Shared() => Kilo2(shared);
        [Benchmark] public void Rent_2_Kestrel() => Kilo2(kestrel);

        static void Kilo1(MemoryPool<byte> pool)
        {
            using (var o = pool.Rent(512))
            {
                o.Memory.Span[0] = 1;
            }
        }

        public static void Kilo2(MemoryPool<byte> pool)
        {
            using (var o1 = pool.Rent(512))
            using (var o2 = pool.Rent(512))
            {
                o1.Memory.Span[0] = 1;
                o2.Memory.Span[0] = 1;
            }
        }

        public void Dispose()
        {
            thruster?.Dispose();
            kestrel?.Dispose();
        }
    }
}