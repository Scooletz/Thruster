using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Thruster.Tests
{
    public class FastMemoryPoolTests
    {
        const int CoreCount = 1;
        static FastMemoryPool<byte> CreatePool() => new FastMemoryPool<byte>(CoreCount);

        [Test]
        public void CanDisposeAfterCreation()
        {
            var memoryPool = CreatePool();
            memoryPool.Dispose();
        }

        [Test]
        public void CanDisposeAfterReturningBlock()
        {
            var memoryPool = CreatePool();
            var block = memoryPool.Rent();
            block.Dispose();
            memoryPool.Dispose();
        }

        [Test]
        public void CanDisposeAfterPinUnpinBlock()
        {
            var memoryPool = CreatePool();
            var block = memoryPool.Rent();
            block.Memory.Pin().Dispose();
            block.Dispose();
            memoryPool.Dispose();
        }

        [Test]
        public void LeasedSegmentsAreConsecutive()
        {
            var size = default(Size4K).GetChunkSize();

            using (var memoryPool = CreatePool())
            using (var o1 = memoryPool.Rent(1))
            using (var o2 = memoryPool.Rent(1))
            {
                Assert.True(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)o1.Memory, out var segment1));
                Assert.True(MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)o2.Memory, out var segment2));

                Assert.AreSame(segment1.Array, segment2.Array);

                var offset1 = segment1.Offset;
                Assert.AreEqual(size, segment1.Count);

                Assert.AreEqual(offset1 + size, segment2.Offset);
                Assert.AreEqual(size, segment2.Count);
            }
        }

        //[Test]
        //public void SaturatingGeneration0MovesAllocationsToGeneration1()
        //{
        //    const int size = FastMemoryPool<byte>.ChunkSize;

        //    using (var memoryPool = CreatePool())
        //    {
        //        var owners = new List<IMemoryOwner<byte>>();
        //        var arrays = new HashSet<byte[]>();

        //        for (var gen = 0; gen < 3; gen++)
        //        {
        //            for (var i = 0; i < 63; i++)
        //            {
        //                var owner = memoryPool.Rent(1);
        //                MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)owner.Memory, out var segment);
        //                arrays.Add(segment.Array);

        //                Assert.AreEqual(size << gen, segment.Count);
        //            }

        //            Assert.AreEqual(gen + 1, arrays.Count);
        //        }
        //    }
        //}

        [Test]
        public void UpAndDown()
        {
            using (var memoryPool = CreatePool())
            {
                var owners = new ConcurrentQueue<IMemoryOwner<byte>>();

                for (int j = 0; j < 10; j++)
                {
                    for (var i = 0; i < 63 + 1; i++)
                    {
                        var owner = memoryPool.Rent(1);
                        owners.Enqueue(owner);
                    }

                    while (owners.TryDequeue(out var o))
                    {
                        o.Dispose();
                    }
                }
            }
        }


        [Test]
        public void LeasingFromDisposedPoolThrows()
        {
            var memoryPool = CreatePool();
            memoryPool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => memoryPool.Rent());
        }
    }
}