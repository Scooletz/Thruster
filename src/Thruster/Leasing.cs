using System.Runtime.CompilerServices;
using System.Threading;

namespace Thruster
{
    /// <summary>
    /// This class is responsible for managing leases of continuous bits 1111 over a ref long. This means that any lease cannot be claimed for more than 63 consecutive elements.
    ///
    /// A lease of specific length n is represented as a n consecutive bits set to 1. This mask is easily calculated: 2^n - 1.
    /// 
    /// Leasing is done by using Interlocked.CompareExchange to swap the previous value with a new value containing n consecutive bits set. This might be retried if another thread/task Leases or Releases a segment.
    /// 
    /// Releasing is done in a much simpler manner. As the lease is represented by a mask at a specific position, we can easily use Interlocked.Add(ref _, -mask) to set these bits back to zeroes.
    /// As there can be only one lease over the specific segment, this operation requires no retries and will always succeed.
    /// 
    /// </summary>
    static class Leasing
    {
        const short NoSpace = -1;
        const short NotFound = -2;

        /// <summary>
        /// Simply calculates 2^n - 1;
        /// </summary>
        /// <param name="continousItems"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long GetMask(int continousItems) => (1L << continousItems) - 1;

        public static short Lease(ref long v, int continousItems, int retries)
        {
            var length = 65 - continousItems;
            var mask = GetMask(continousItems);

            // initially, the value is read from ref v. Later, if leasing fails, is obtained from CompareExchange.
            var value = Volatile.Read(ref v);

            for (var approach = 0; approach < retries; approach++)
            {
                var newValue = value;
                var i = 0;
                for (; i < length; i++)
                {
                    if ((value & (mask << i)) == 0)
                    {
                        newValue |= mask << i;
                        break;
                    }
                }

                // if there's no free space in v, just return
                if (i == length)
                {
                    return NoSpace;
                }

                var result = Interlocked.CompareExchange(ref v, newValue, value);
                if (result == value)
                {
                    return (short) i;
                }

                // leasing attempt failed, retry with the recently obtained value
                value = result;
            }

            return NotFound;
        }

        public static void Release(ref long v, int continousItems, short lease)
        {
            var value = GetMask(continousItems);
            value <<= lease;
            Interlocked.Add(ref v, -value);
        }
    }
}