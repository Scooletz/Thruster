using System;
using System.Buffers;
using BenchmarkDotNet.Attributes;

namespace Thruster.Benchmarks
{
    public class AgainstSharedPool : IDisposable
    {
        readonly MemoryPool<byte> _circular;
        readonly MemoryPool<byte> _shared;

        public AgainstSharedPool()
        {
            _circular = new FastMemoryPool<byte>();
            _shared = MemoryPool<byte>.Shared;
        }

        [Benchmark] public void Mine_1_Rent() => Kilo1(_circular);
        [Benchmark] public void Mine_2_nested_Rents() => Kilo2(_circular);
        [Benchmark] public void Mine_3_nested_Rents() => Kilo3(_circular);

        [Benchmark] public void Shared_1_Rent() => Kilo1(_shared);
        [Benchmark] public void Shared_2_nested_Rents() => Kilo2(_shared);
        [Benchmark] public void Shared_3_nested_Rents() => Kilo3(_shared);

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

        public static void Kilo3(MemoryPool<byte> pool)
        {
            using (var o1 = pool.Rent(512))
            using (var o2 = pool.Rent(512))
            using (var o3 = pool.Rent(512))
            {
                o1.Memory.Span[0] = 1;
                o2.Memory.Span[0] = 1;
                o3.Memory.Span[0] = 1;
            }
        }

        public void Dispose()
        {
            _circular?.Dispose();
            _shared?.Dispose();
        }
    }
}