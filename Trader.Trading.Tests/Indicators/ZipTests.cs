using Outcompute.Trader.Indicators;

namespace Outcompute.Trader.Trading.Tests.Indicators
{
    public static class ZipTests
    {
        [Fact]
        public static void YieldsOutput()
        {
            // arrange
            using var first = new Identity<decimal?> { 1, 2, 3 };
            using var second = new Identity<decimal?> { 2, 3, 4 };

            // act
            using var indicator = Indicator.Zip(first, second, (x, y) => x + y);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(3, x),
                x => Assert.Equal(5, x),
                x => Assert.Equal(7, x));
        }

        [Fact]
        public static void UpdatesFromSource()
        {
            // arrange
            using var first = new Identity<decimal?> { 1, 2, 3 };
            using var second = new Identity<decimal?> { 2, 3, 4 };
            using var indicator = Indicator.Zip(first, second, (x, y) => x + y);

            // act
            first.Update(1, 5);
            second.Update(2, 9);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(3, x),
                x => Assert.Equal(8, x),
                x => Assert.Equal(12, x));
        }
    }
}