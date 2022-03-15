using Outcompute.Trader.Trading.Indicators;

namespace Outcompute.Trader.Trading.Tests
{
    public static class AbsIndicatorTests
    {
        [Fact]
        public static void YieldsEmptyOutput()
        {
            // act
            var indicator = new AbsIndicator();

            // assert
            Assert.Empty(indicator);
        }

        [Fact]
        public static void YieldsAbsoluteOutput()
        {
            // act
            var indicator = new AbsIndicator
            {
                1, -1, 2, -3, 5, -8, 13, -21, 34, -55, 89, -144
            };

            // assert
            Assert.True(indicator.SequenceEqual(new decimal?[] { 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 }));
        }
    }
}