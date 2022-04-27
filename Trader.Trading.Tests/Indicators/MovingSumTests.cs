using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests
{
    public class MovingSumTests
    {
        [Fact]
        public void YieldsMovingSum()
        {
            // arrange
            using var input = new Identity<decimal?>() { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };

            // act
            using var indicator = new MovingSum(input, 3);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(6, x),
                x => Assert.Equal(10, x),
                x => Assert.Equal(16, x),
                x => Assert.Equal(26, x),
                x => Assert.Equal(42, x),
                x => Assert.Equal(68, x),
                x => Assert.Equal(110, x),
                x => Assert.Equal(178, x),
                x => Assert.Equal(288, x));
        }
    }
}