using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Thruster.Tests
{
    public class FastMemoryPoolTests
    {
        static FastMemoryPool<byte> CreatePool() => new FastMemoryPool<byte>();

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
            const int size = FastMemoryPool<byte>.ChunkSize;

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

        [Test]
        public void LeasingFromDisposedPoolThrows()
        {
            var memoryPool = CreatePool();
            memoryPool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => memoryPool.Rent());
        }
    }
}