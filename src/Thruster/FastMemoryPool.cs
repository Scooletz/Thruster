using System;
using System.Buffers;
using System.Threading;

namespace Thruster
{
    public class FastMemoryPool<T> : MemoryPool<T>
    {
        internal const int ChunkSize = 4 * 1024;
        internal const int ChunkSizeLog = 12;
        internal const int ChunksPerCpu = 64;
        internal const int MaxGen = 3;
        const int LeasingOffset = Util.CacheLineSize / 8;

        readonly int processorCount;
        readonly long[] leasing;
        readonly T[] gen0;
        volatile T[] gen1;
        volatile T[] gen2;

        bool disposed;

        public FastMemoryPool()
            : this(Math.Min(Environment.ProcessorCount, 64))
        {
        }

        internal FastMemoryPool(int processorCount)
        {
            this.processorCount = processorCount;
            var allocSize = GetAllocSize(0);
            leasing = new long[(this.processorCount + 2) * LeasingOffset];

            gen0 = new T[allocSize];
        }

        int GetAllocSize(int gen) => (processorCount * ChunksPerCpu * ChunkSize) << gen;

        public override IMemoryOwner<T> Rent(int size = -1)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("MemoryPool has been disposed.");
            }

            if (size <= 0)
            {
                size = 1;
            }

            var capacity = size.AlignToMultipleOf(ChunkSize);
            var chunkCount = capacity >> ChunkSizeLog;

            var processorId = GetProcessorId();

            //try local processor first
            var owner = Lease(processorId, chunkCount, 0);
            if (owner != null)
            {
                return owner;
            }

            return LeaseSlowPath(processorId, chunkCount);
        }

        Owner Lease(int processorId, int chunkCount, int gen)
        {
            var index = (short)((processorId + 1) * LeasingOffset);
            ref var slot = ref leasing[index + gen];

            var lease = Leasing.Lease(ref slot, chunkCount, 3);
            if (lease >= 0)
            {
                var chunkShift = ChunkSizeLog + gen;
                var offset = (processorId * ChunksPerCpu + lease) << chunkShift;
                return new Owner(new Memory<T>(GetGen(gen), offset, chunkCount << chunkShift), index, lease, leasing);
            }

            return default;
        }

        IMemoryOwner<T> LeaseSlowPath(int processorId, int chunkCount)
        {
            var spin = new SpinWait();

            for (var gen = 0; gen < MaxGen; gen++)
            {
                for (var i = 0; i < processorCount; i++)
                {
                    spin.SpinOnce();
                    processorId = (processorId + i) % processorCount;

                    var owner = Lease(processorId, chunkCount, gen);
                    if (owner != null)
                    {
                        return owner;
                    }
                }
            }

            // allocate if none is found
            return new Owner(new Memory<T>(new T[chunkCount * ChunkSize]), 0, 0, null);
        }

        public override int MaxBufferSize => 32 * ChunkSize; // half of the max is provided

        protected override void Dispose(bool disposing)
        {
            disposed = true;
        }

        class Owner : IMemoryOwner<T>
        {
            readonly short leasingIndex;
            readonly short lease;
            readonly long[] leasing;

            public Owner(Memory<T> memory, short leasingIndex, short lease, long[] leasing)
            {
                Memory = memory;
                this.leasingIndex = leasingIndex;
                this.lease = lease;
                this.leasing = leasing;
            }

            public void Dispose()
            {
                if (leasing != null)
                {
                    Leasing.Release(ref leasing[leasingIndex], Memory.Length / ChunkSize, lease);
                }
            }

            public Memory<T> Memory { get; }
        }

        T[] GetGen(int gen)
        {
            switch (gen)
            {
                case 0:
                    return gen0;
                case 1:
                    if (gen1 == null)
                    {
                        lock (gen0)
                        {
                            if (gen1 == null)
                            {
                                gen1 = new T[GetAllocSize(1)];
                            }
                        }
                    }
                    return gen1;
                case 2:
                    if (gen2 == null)
                    {
                        lock (gen0)
                        {
                            if (gen2 == null)
                            {
                                gen2 = new T[GetAllocSize(2)];
                            }
                        }
                    }
                    return gen2;
                default:
                    return default;
            }
        }

        int GetProcessorId()
        {
            int processorId;
#if NETCOREAPP2_1
            processorId = Thread.GetCurrentProcessorId();
            if (processorId < 0)
            {
                processorId = Environment.CurrentManagedThreadId;
            }
#else
            processorId = Environment.CurrentManagedThreadId;
#endif
            // Add offset to make it clear that it is not guaranteed to be 0-based processor number 
            processorId += 100;

            return processorId % processorCount;
        }
    }

}