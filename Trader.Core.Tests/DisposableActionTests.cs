using Xunit;

namespace Outcompute.Trader.Core.Tests
{
    public class DisposableActionTests
    {
        [Fact]
        public void ActionOnDispose()
        {
            // arrange
            var okay = false;
            var action = new DisposableAction(() => okay = true);

            // act
            action.Dispose();

            // assert
            Assert.True(okay);
        }
    }
}