using System;
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
        public void LeasingFromDisposedPoolThrows()
        {
            var memoryPool = CreatePool();
            memoryPool.Dispose();

            Assert.Throws<ObjectDisposedException>(() => memoryPool.Rent());
        }
    }
}