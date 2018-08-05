using NUnit.Framework;

namespace Thruster.Tests
{
    public class LeasingTests
    {
        [Test]
        public void Lease_and_Release_Should_leave_all_bits_0([Values(1, 32, 33, 63)]int consecutive)
        {
            var v = 0L;

            var lease = Leasing.Lease(ref v, consecutive, 1);
            Assert.AreEqual(0, lease);

            Leasing.Release(ref v, consecutive, lease);
            
            Assert.AreEqual(0L, v);
        }

        [TestCase(0x0000_0000_0000_0001, 1)]
        [TestCase(0x0000_0000_0000_0003, 2)]
        [TestCase(0x0000_0000_0000_FFFF, 16)]
        [TestCase(0x0000_0000_00FF_FFFF, 24)]
        [TestCase(0x7FFF_FFFF_FFFF_FFFF, 63)]
        public void Lease_Should_find_first_gap(long value, short expected)
        {
            const int consecutive = 1;
            var v = value;
            var lease = Leasing.Lease(ref v, consecutive, 1);

            Assert.AreEqual(expected, lease);

            Leasing.Release(ref v, consecutive, lease);

            Assert.AreEqual(value, v);
        }
    }
}