using Ara.Core;
using Xunit;

namespace Ara.Tests
{
    public class SupervisorAuthTests
    {
        [Fact]
        public void HashPinIsStableSha256()
        {
            var hash = SupervisorAuth.HashPin("123456");
            Assert.Equal(64, hash.Length);
            Assert.Equal(hash, SupervisorAuth.HashPin("123456"));
            Assert.NotEqual(hash, SupervisorAuth.HashPin("123457"));
        }

        [Fact]
        public void CorrectPinUnlocks()
        {
            var auth = new SupervisorAuth(SupervisorAuth.HashPin("246810"));
            Assert.True(auth.IsConfigured);
            Assert.True(auth.Verify("246810"));
            Assert.False(auth.IsLockedOut);
        }

        [Fact]
        public void WrongPinDoesNotUnlock()
        {
            var auth = new SupervisorAuth(SupervisorAuth.HashPin("246810"));
            Assert.False(auth.Verify("000000"));
        }

        [Fact]
        public void ThreeWrongPinsTriggerLockout()
        {
            var auth = new SupervisorAuth(SupervisorAuth.HashPin("246810"));
            Assert.False(auth.Verify("1"));
            Assert.False(auth.Verify("2"));
            Assert.False(auth.Verify("3"));
            Assert.True(auth.IsLockedOut);
            Assert.False(auth.Verify("246810"));
            Assert.True(auth.RemainingLockoutSeconds > 0);
        }

        [Fact]
        public void EmptyHashDisablesSupervisorPanel()
        {
            var auth = new SupervisorAuth(string.Empty);
            Assert.False(auth.IsConfigured);
            Assert.False(auth.Verify(string.Empty));
        }
    }
}
