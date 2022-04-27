using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests
{
    public class GainTests
    {
        [Fact]
        public void YieldsPositiveChanges()
        {
            // arrange
            using var identity = Indicator.Identity<decimal?>(1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144);
            using var indicator = Indicator.Gain(identity);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(2, x),
                x => Assert.Equal(3, x),
                x => Assert.Equal(5, x),
                x => Assert.Equal(8, x),
                x => Assert.Equal(13, x),
                x => Assert.Equal(21, x),
                x => Assert.Equal(34, x),
                x => Assert.Equal(55, x));
        }

        [Fact]
        public void YieldsNegativeChanges()
        {
            // arrange
            using var identity = Indicator.Identity<decimal?>(144, 89, 55, 34, 21, 13, 8, 5, 3, 2, 1, 1);
            using var indicator = Indicator.Gain(identity);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(0, x));
        }

        [Fact]
        public void YieldsMixedChanges()
        {
            // arrange
            using var identity = Indicator.Identity<decimal?>(1, 2, 1, 5, 3, 13, 8, 34, 21, 89, 55, 144);
            using var indicator = Indicator.Gain(identity);

            // assert
            Assert.Collection(indicator,
                x => Assert.Null(x),
                x => Assert.Equal(1, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(10, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(26, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(68, x),
                x => Assert.Equal(0, x),
                x => Assert.Equal(89, x));
        }
    }
}