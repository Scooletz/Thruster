using NUnit.Framework;

namespace Thruster.Tests
{
    public class LeasingTests
    {
        [Test]
        public void LeaseAndReleaseShouldLeaveAllBitsUnset([Values(1, 32, 33, 63)]int consecutive)
        {
            var v = 0L;

            var lease = Leasing.Lease(ref v, consecutive, 1);
            Assert.AreEqual(0, lease);

            Leasing.Release(ref v, consecutive, lease);

            Assert.AreEqual(0L, v);
        }

        [TestCase(0x0000_0000_0000_0000, 0)]
        [TestCase(0x0000_0000_0000_0001, 1)]
        [TestCase(0x0000_0000_0000_0003, 2)]
        [TestCase(0x0000_0000_0000_FFFF, 16)]
        [TestCase(0x0000_0000_00FF_FFFF, 24)]
        [TestCase(0x3FFF_FFFF_FFFF_FFFF, 62)]
        [TestCase(0x7FFF_FFFF_FFFF_FFFF, Leasing.NoSpace)]
        public void LeaseShouldFindFirstMatchingGap(long value, short expected)
        {
            const int consecutive = 1;
            var v = value;
            var lease = Leasing.Lease(ref v, consecutive, 1);

            Assert.AreEqual(expected, lease);

            if (lease >= 0)
            {
                Leasing.Release(ref v, consecutive, lease);
                Assert.AreEqual(value, v);
            }
        }

        [Test]
        public void LeaseShouldReturnNegativeNumberWhenNoSpace()
        {
            var i = ~0L;

            var lease = Leasing.Lease(ref i, 1, 1);

            Assert.AreEqual(-1, lease);
        }
    }
}