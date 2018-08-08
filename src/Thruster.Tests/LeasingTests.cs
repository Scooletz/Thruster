using NUnit.Framework;

namespace Thruster.Tests
{
    public class LeasingTests
    {
        [Test]
        public void LeaseAndReleaseShouldLeaveAllBitsUnset([Values(1, 32, 33, 63)]int consecutive)
        {
            var leasing = new Leasing(1);

            var lease = Leasing.Lease(ref leasing, 0, consecutive, 1);
            Assert.AreEqual(0, lease);

            Leasing.Release(ref leasing, 0, consecutive, (short)lease);

            lease = Leasing.Lease(ref leasing, 0, consecutive, 1);
            Assert.AreEqual(0, lease);
        }
    }
}