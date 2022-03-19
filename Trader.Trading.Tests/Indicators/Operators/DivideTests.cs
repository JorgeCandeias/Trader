using Outcompute.Trader.Trading.Indicators;
using Outcompute.Trader.Trading.Indicators.Operators;

namespace Outcompute.Trader.Trading.Tests.Indicators.Operators
{
    public static class DivideTests
    {
        [Fact]
        public static void YieldsOutput()
        {
            // arrange
            using var first = new Identity<decimal?> { 4, 6, 8, null, 1 };
            using var second = new Identity<decimal?> { 2, 2, 2, 1, null };

            // act
            using var indicator = new Divide(first, second);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(2, x),
                x => Assert.Equal(3, x),
                x => Assert.Equal(4, x),
                x => Assert.Null(x),
                x => Assert.Null(x));
        }

        [Fact]
        public static void UpdatesFromSource()
        {
            // arrange
            using var first = new Identity<decimal?> { 4, 6, 8, null, 1 };
            using var second = new Identity<decimal?> { 2, 2, 2, 1, null };
            using var indicator = new Divide(first, second);

            // act
            first.Update(1, 8);
            second.Update(2, 4);

            // assert
            Assert.Collection(indicator,
                x => Assert.Equal(2, x),
                x => Assert.Equal(4, x),
                x => Assert.Equal(2, x),
                x => Assert.Null(x),
                x => Assert.Null(x));
        }
    }
}